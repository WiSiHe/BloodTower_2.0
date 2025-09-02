using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// Boss push duel: hero and king close the gap, then move as a locked block left/right
/// based on the mash rate + biases. Uses colliders for width (or fallbacks).
/// Wins when the *block* reaches an arena edge.
/// Optional input: SimpleMashInput (exposes TapRate).
[DefaultExecutionOrder(-20)]
public class BossMashDuel : MonoBehaviour
{
    [Header("Scene Refs")]
    public Transform hero;
    public Transform king;
    public Rigidbody2D heroRB;     // optional; movement will set transform either way
    public Rigidbody2D kingRB;     // optional

    [Header("Mash Input (optional)")]
    public SimpleMashInput mash;   // any script exposing TapRate (float taps per second)

    [Header("Arena")]
    public Transform midpoint;     // center of the platform
    [Tooltip("Half of the playable width, from midpoint to an edge.")]
    public float halfSpan = 4f;
    [Tooltip("If true, spawn near edges (inset inward by StartInset).")]
    public bool startAtEdges = true;
    [Tooltip("Inset from each edge when startAtEdges=true.")]
    public float startInset = 0.6f;
    [Tooltip("Fixed Y world position for both. Leave 0 to keep current.")]
    public float yPos = 0f;
    [Tooltip("If true, hero begins on the LEFT; otherwise hero begins on the RIGHT.")]
    public bool heroStartsLeft = true;

    [Header("Tuning")]
    [Tooltip("Player push = TapRate * PlayerPushPerRate + (Saved-Killed) * AdvantagePerKid")]
    public float playerPushPerRate = 0.25f;
    [Tooltip("Baseline king push (per second).")]
    public float kingBasePush = 6.0f;
    [Tooltip("King gets stronger as fight progresses (multiplier per second).")]
    public float kingRampUpRate = 0.4f;
    [Tooltip("Maximum king strength multiplier.")]
    public float kingMaxMultiplier = 5.0f;
    [Tooltip("Positive helps hero, negative helps king; multiplied by (Saved-Killed).")]
    public float advantagePerKid = 0.03f;
    [Tooltip("Tiny constant drift each second; >0 nudges to the RIGHT, <0 to LEFT.")]
    public float constantDrift = -0.1f;
    [Tooltip("How sticky the last bit near edges feels.")]
    public float edgeSlug = 0.8f;
    [Tooltip("Speed at which characters approach each other before clashing (units per second).")]
    public float approachSpeed = 2.0f;
    
    [Header("Blood Power System")]
    [Tooltip("How much consuming children weakens the king (per child consumed).")]
    public float bloodWeakeningPerChild = 0.25f;
    [Tooltip("Maximum king weakness from blood power (caps the effect).")]
    public float maxBloodWeakening = 0.8f;
    [Tooltip("Bonus player strength per child consumed.")]
    public float bloodStrengthPerChild = 0.15f;
    [Tooltip("Maximum player strength bonus from blood power.")]
    public float maxBloodStrength = 0.8f;
    
    [Header("Responsiveness")]
    [Tooltip("Instant push bonus for each button press (adds immediate feedback).")]
    public float instantPushBonus = 0.01f;
    [Tooltip("How long the instant push bonus lasts (seconds).")]
    public float instantPushDuration = 0.03f;
    [Tooltip("Momentum builds up from sustained mashing, multiplying push effectiveness.")]
    public float maxMomentumMultiplier = 1.08f;
    [Tooltip("How quickly momentum builds up (0-1 per second).")]
    public float momentumBuildRate = 0.3f;
    [Tooltip("How quickly momentum decays when not mashing (0-1 per second).")]
    public float momentumDecayRate = 12.0f;

    [Header("Separation")]
    [Tooltip("Use collider widths when available.")]
    public bool useColliderWidths = true;
    [Tooltip("Fallback half-width if hero has no collider.")]
    public float heroHalfWidthFallback = 0.40f;
    [Tooltip("Fallback half-width if king has no collider.")]
    public float kingHalfWidthFallback = 0.80f;
    [Tooltip("Extra air gap beyond colliders once locked (0 = shoulder-to-shoulder).")]
    public float minGap = 0.10f;

    [Header("Win / UX")]
    [Tooltip("Delay before routing after win (seconds).")]
    public float postWinDelay = 0.35f;

    [Header("Debug")]
    public bool debugLogs = true;

    // --- runtime ---
    float _heroHalf, _kingHalf;
    float _leftEdge, _rightEdge;             // raw edges
    float _leftClampHero, _rightClampHero;   // edges minus/plus half widths & slug
    float _leftClampKing, _rightClampKing;

    float heroX, kingX;                      // centers
    float _minCenterGap;                     // colliders + minGap
    
    // Responsiveness system
    float _instantPushRemaining = 0f;        // current instant push bonus
    float _momentum = 0f;                    // current momentum multiplier (0-1)
    float _lastTapRate = 0f;                 // for detecting input changes
    
    // King progressive difficulty
    float _fightStartTime = 0f;              // when the fight began
    float _kingStrengthMultiplier = 1f;      // current king strength
    
    // Blood power system
    float _bloodPowerWeakening = 0f;         // king weakness from consumed children
    float _bloodPowerStrength = 0f;          // player strength bonus from consumed children

    // If your SimpleMashInput exposes TapRate, we'll read it.
    float TapRate => mash ? mash.TapRate : 0f;

    /// <summary>
    /// Progress of the duel from 0 to 1, where 0.5 is center/start position.
    /// 0 = hero pushed to left edge, 1 = hero pushed to right edge.
    /// </summary>
    public float Progress01
    {
        get
        {
            if (!hero || !king || midpoint == null) return 0.5f;
            
            // Get the center point between hero and king
            float blockCenter = (heroX + kingX) * 0.5f;
            float arenaCenter = midpoint.position.x;
            
            // Convert to 0-1 range where 0.5 is center
            float progress = (blockCenter - arenaCenter) / halfSpan * 0.5f + 0.5f;
            return Mathf.Clamp01(progress);
        }
    }
    
    /// <summary>
    /// Current momentum value (0-1) for visual effects.
    /// </summary>
    public float CurrentMomentum => _momentum;
    
    /// <summary>
    /// Current king strength multiplier (starts at 1, increases over time).
    /// </summary>
    public float KingStrength => _kingStrengthMultiplier;
    
    /// <summary>
    /// How much the king is weakened by blood power (0-1).
    /// </summary>
    public float BloodPowerWeakening => _bloodPowerWeakening;
    
    /// <summary>
    /// How much the player is strengthened by blood power (0-1).
    /// </summary>
    public float BloodPowerStrength => _bloodPowerStrength;
    
    /// <summary>
    /// Whether player has instant push remaining (for visual/audio feedback).
    /// </summary>
    public bool HasInstantPush => _instantPushRemaining > 0.01f;
    
    /// <summary>
    /// Intensity of the current struggle (0-1) for screen shake, etc.
    /// </summary>
    public float StruggleIntensity
    {
        get
        {
            if (!hero || !king) return 0f;
            float gap = heroStartsLeft ? (kingX - heroX) : (heroX - kingX);
            bool touching = gap <= _minCenterGap + 1e-3f;
            if (!touching) return 0f;
            
            // Higher intensity when forces are more balanced and near edges
            float forceBalance = Mathf.Min(TapRate * playerPushPerRate, kingBasePush * 2f) / Mathf.Max(TapRate * playerPushPerRate, kingBasePush * 2f);
            float edgeProximity = 1f - Mathf.Abs(Progress01 - 0.5f) * 2f; // closer to edges = higher
            return forceBalance * edgeProximity * _momentum;
        }
    }

    void OnValidate()
    {
        // keep some sanity ranges
        halfSpan     = Mathf.Max(0.1f, halfSpan);
        startInset   = Mathf.Max(0f, startInset);
        edgeSlug     = Mathf.Max(0f, edgeSlug);
        heroHalfWidthFallback = Mathf.Max(0.02f, heroHalfWidthFallback);
        kingHalfWidthFallback = Mathf.Max(0.02f, kingHalfWidthFallback);
        minGap = Mathf.Max(0f, minGap);
    }

    void Awake()
    {
        AutofindWidths();
        ComputeEdges();
    }

    void Start()
    {
        // place them
        if (startAtEdges)
            PlaceAtEdges();
        else
            ReadCurrentPositions();

        // snap to Y if specified
        if (yPos != 0f)
        {
            if (hero) hero.position = new Vector3(hero.position.x, yPos, hero.position.z);
            if (king) king.position = new Vector3(king.position.x, yPos, king.position.z);
        }

        // Initialize fight timing
        _fightStartTime = Time.time;
        _kingStrengthMultiplier = 1f;
        
        // Initialize blood power system
        CalculateBloodPower();

        if (debugLogs)
        {
            int consumed = GameSession.Instance ? GameSession.Instance.ChildrenKilled : 0;
            Debug.Log($"[BossMashDuel] INIT mid={(midpoint?midpoint.position.x:0f):F2} | span={halfSpan*2f:F2} | " +
                      (heroStartsLeft
                        ? $"heroX={heroX:F2} | kingX={kingX:F2}"
                        : $"kingX={kingX:F2} | heroX={heroX:F2}") + 
                      $" | consumed={consumed} | bloodWeak={_bloodPowerWeakening:P0} | bloodStr={_bloodPowerStrength:P0}");
        }
    }

    void Update()
    {
        if (!hero || !king || midpoint == null) return;

        // 1) Process input responsiveness
        float taps = Mathf.Max(0f, TapRate); // taps / second
        
        // Detect input spikes for instant feedback (more sensitive at higher difficulty)
        float spikeThreshold = Mathf.Lerp(0.5f, 1.5f, (_kingStrengthMultiplier - 1f) / (kingMaxMultiplier - 1f));
        if (taps > _lastTapRate + spikeThreshold) // significant input increase
        {
            _instantPushRemaining = Mathf.Max(_instantPushRemaining, instantPushBonus);
        }
        _lastTapRate = taps;
        
        // Update momentum based on sustained input (nearly impossible to maintain without blood power)
        float requiredTapRate = Mathf.Lerp(4.0f, 15.0f, _momentum * _momentum); // insane requirement, need way more taps
        if (taps > requiredTapRate) // actively mashing at required rate
        {
            float buildPenalty = Mathf.Lerp(1f, 0.1f, _momentum); // builds extremely slowly at high momentum
            _momentum = Mathf.Min(1f, _momentum + momentumBuildRate * buildPenalty * Time.deltaTime);
        }
        else // not mashing enough
        {
            float decayMultiplier = Mathf.Lerp(1f, 8f, _momentum); // decay catastrophically fast at higher momentum
            _momentum = Mathf.Max(0f, _momentum - momentumDecayRate * decayMultiplier * Time.deltaTime);
        }
        
        // Decay instant push
        _instantPushRemaining = Mathf.Max(0f, _instantPushRemaining - Time.deltaTime / instantPushDuration);

        // 2) Update king strength over time (progressive difficulty)
        float fightDuration = Time.time - _fightStartTime;
        _kingStrengthMultiplier = Mathf.Min(kingMaxMultiplier, 1f + fightDuration * kingRampUpRate);
        
        // Add king rubber-band effect: absolutely crushing when player is ahead
        float currentProgress = Progress01;
        float playerAdvantage = Mathf.Max(0f, currentProgress - 0.5f) * 2f; // 0-1 how much player is ahead
        float rubberBandBonus = playerAdvantage * playerAdvantage * playerAdvantage * 4.0f; // up to 400% bonus when player is near winning (cubic growth)
        _kingStrengthMultiplier += rubberBandBonus;
        
        // 3) Calculate enhanced pushes
        int saved  = GameSession.Instance ? GameSession.Instance.ChildrenSaved  : 0;
        int killed = GameSession.Instance ? GameSession.Instance.ChildrenKilled : 0;
        float kidAdv = (saved - killed) * advantagePerKid;
        
        // Apply momentum multiplier to base push
        float momentumMult = Mathf.Lerp(1f, maxMomentumMultiplier, _momentum);
        float basePush = taps * playerPushPerRate * momentumMult;
        
        // Apply blood power effects
        float playerPushPerSec = (basePush + kidAdv + _instantPushRemaining) * (1f + _bloodPowerStrength);    // + = to the RIGHT
        float kingPushPerSec   = kingBasePush * _kingStrengthMultiplier * (1f - _bloodPowerWeakening);         // = to the LEFT if hero-left

        // convert to signed shift (RIGHT positive, LEFT negative)
        float signedShift = SignedShift(playerPushPerSec, kingPushPerSec, heroStartsLeft);
        signedShift += constantDrift; // bias
        float shift = signedShift * Time.deltaTime;

        // 4) Approach → Lock block
        float currentGap = heroStartsLeft ? (kingX - heroX) : (heroX - kingX);
        bool touching = currentGap <= _minCenterGap + 1e-3f;

        float newHeroX = heroX;
        float newKingX = kingX;

        if (!touching)
        {
            // distance we still need to close
            float remaining = Mathf.Max(0f, currentGap - _minCenterGap);
            
            // Use dedicated approach speed for closing the gap
            float approachDistance = approachSpeed * Time.deltaTime;
            float toUse = Mathf.Min(approachDistance, remaining);
            
            // step toward each other
            if (heroStartsLeft)
            {
                newHeroX += 0.5f * toUse;   // right
                newKingX -= 0.5f * toUse;   // left
            }
            else
            {
                newHeroX -= 0.5f * toUse;   // left
                newKingX += 0.5f * toUse;   // right
            }
            
            // any leftover approach distance can be used for push effects
            float excess = approachDistance - toUse;
            if (excess > 0f && Mathf.Abs(shift) > 0f)
            {
                float dir = Mathf.Sign(shift);
                float pushEffect = Mathf.Min(excess, Mathf.Abs(shift));
                newHeroX += dir * pushEffect;
                newKingX += dir * pushEffect;
            }
        }
        else
        {
            // move as a block
            newHeroX += shift;
            newKingX += shift;
            // re-enforce exact minimal gap
            if (heroStartsLeft)
                newKingX = Mathf.Max(newKingX, newHeroX + _minCenterGap);
            else
                newHeroX = Mathf.Max(newHeroX, newKingX + _minCenterGap);
        }

        // 5) Clamp to arena (with widths + slug)
        newHeroX = Mathf.Clamp(newHeroX, _leftClampHero, _rightClampHero);
        newKingX = Mathf.Clamp(newKingX, _leftClampKing, _rightClampKing);

        // 6) Apply
        heroX = newHeroX;
        kingX = newKingX;
        ApplyPositions();

        if (debugLogs)
        {
            float dist = heroStartsLeft ? (kingX - heroX) : (heroX - kingX);
            int consumed = GameSession.Instance ? GameSession.Instance.ChildrenKilled : 0;
            float requiredRate = Mathf.Lerp(4.0f, 15.0f, _momentum * _momentum);
            float progress = Progress01;
            float debugAdvantage = Mathf.Max(0f, progress - 0.5f) * 2f;
            float rubberBand = debugAdvantage * debugAdvantage * debugAdvantage * 4.0f;
            Debug.Log($"[MASH] rate={taps:F2} (need={requiredRate:F2}) | momentum={_momentum:F2} | instant={_instantPushRemaining:F2} | " +
                      $"pushH={playerPushPerSec:F2} | pushK={kingPushPerSec:F2} | kingStr={_kingStrengthMultiplier:F2} (rubber={rubberBand:F2}) | " +
                      $"progress={progress:F2} | bloodWeak={_bloodPowerWeakening:F2} | bloodStr={_bloodPowerStrength:F2} | consumed={consumed} | dist={dist:F2}");
        }

        // 7) Win checks (only meaningful when touching)
        touching = (heroStartsLeft ? (kingX - heroX) : (heroX - kingX)) <= _minCenterGap + 1e-3f;
        if (touching && TryWin()) return;
    }

    // --------------------------------------------------------
    // Helpers
    // --------------------------------------------------------
    float SignedShift(float playerPushPerSec, float kingPushPerSec, bool heroLeft)
    {
        // Convention: +RIGHT, -LEFT for world X
        if (heroLeft)
        {
            // hero pushes RIGHT, king pushes LEFT
            return (playerPushPerSec - kingPushPerSec);
        }
        else
        {
            // hero on RIGHT: hero pushes LEFT, king pushes RIGHT
            return (-playerPushPerSec + kingPushPerSec);
        }
    }

    void AutofindWidths()
    {
        _heroHalf = heroHalfWidthFallback;
        _kingHalf = kingHalfWidthFallback;

        if (useColliderWidths)
        {
            // Try Collider2D first; else SpriteRenderer bounds.
            if (hero)
            {
                var c = hero.GetComponentInChildren<Collider2D>();
                if (c) _heroHalf = Mathf.Max(_heroHalf, c.bounds.extents.x);
                else
                {
                    var sr = hero.GetComponentInChildren<SpriteRenderer>();
                    if (sr) _heroHalf = Mathf.Max(_heroHalf, sr.bounds.extents.x);
                }
            }
            if (king)
            {
                var c = king.GetComponentInChildren<Collider2D>();
                if (c) _kingHalf = Mathf.Max(_kingHalf, c.bounds.extents.x);
                else
                {
                    var sr = king.GetComponentInChildren<SpriteRenderer>();
                    if (sr) _kingHalf = Mathf.Max(_kingHalf, sr.bounds.extents.x);
                }
            }
        }

        _minCenterGap = Mathf.Max(minGap, _heroHalf + _kingHalf);
    }

    void ComputeEdges()
    {
        float mid = midpoint ? midpoint.position.x : 0f;
        _leftEdge  = mid - halfSpan;
        _rightEdge = mid + halfSpan;

        // clamps (keep some sticky slug)
        _leftClampHero  = _leftEdge  + _heroHalf + edgeSlug;
        _rightClampHero = _rightEdge - _heroHalf - edgeSlug;
        _leftClampKing  = _leftEdge  + _kingHalf + edgeSlug;
        _rightClampKing = _rightEdge - _kingHalf - edgeSlug;
    }

    void PlaceAtEdges()
    {
        float yHero = (yPos != 0f) ? yPos : (hero ? hero.position.y : 0f);
        float yKing = (yPos != 0f) ? yPos : (king ? king.position.y : 0f);

        if (heroStartsLeft)
        {
            heroX = Mathf.Clamp(_leftEdge + _heroHalf + edgeSlug + startInset, _leftClampHero, _rightClampHero);
            kingX = Mathf.Clamp(_rightEdge - _kingHalf - edgeSlug - startInset, _leftClampKing, _rightClampKing);
        }
        else
        {
            kingX = Mathf.Clamp(_leftEdge + _kingHalf + edgeSlug + startInset, _leftClampKing, _rightClampKing);
            heroX = Mathf.Clamp(_rightEdge - _heroHalf - edgeSlug - startInset, _leftClampHero, _rightClampHero);
        }

        if (hero) hero.position = new Vector3(heroX, yHero, hero.position.z);
        if (king) king.position = new Vector3(kingX, yKing, king.position.z);
    }

    void ReadCurrentPositions()
    {
        heroX = hero ? hero.position.x : 0f;
        kingX = king ? king.position.x : 0f;
        if (heroStartsLeft && heroX > kingX)
            SwapSides();
        if (!heroStartsLeft && heroX < kingX)
            SwapSides();
    }

    void SwapSides()
    {
        // swap references in place if they’re reversed
        var tmpT = hero; hero = king; king = tmpT;
        var tmpR = heroRB; heroRB = kingRB; kingRB = tmpR;
        // keep “heroStartsLeft” meaning as set; we swapped objects so the logic still holds
    }

    void ApplyPositions()
    {
        if (heroRB)
        {
            heroRB.linearVelocity = Vector2.zero;
            heroRB.position = new Vector2(heroX, (yPos != 0f) ? yPos : heroRB.position.y);
        }
        else if (hero)
        {
            hero.position = new Vector3(heroX, (yPos != 0f) ? yPos : hero.position.y, hero.position.z);
        }

        if (kingRB)
        {
            kingRB.linearVelocity = Vector2.zero;
            kingRB.position = new Vector2(kingX, (yPos != 0f) ? yPos : kingRB.position.y);
        }
        else if (king)
        {
            king.position = new Vector3(kingX, (yPos != 0f) ? yPos : king.position.y, king.position.z);
        }
    }

    bool TryWin()
    {
        const float EPS = 0.0005f;

        // Determine who is the left/right character *right now*
        bool heroIsLeft = heroX <= kingX;

        // compute current clamps again (already done in ComputeEdges, but re-assert here)
        // hero lose if he hits left clamp; king lose if he hits right clamp — but we decide “winner”.
        bool heroAtLeft  = heroX <= _leftClampHero  + EPS;
        bool heroAtRight = heroX >= _rightClampHero - EPS;
        bool kingAtLeft  = kingX <= _leftClampKing  + EPS;
        bool kingAtRight = kingX >= _rightClampKing - EPS;

        // Who wins?
        // If the LEFT character touches the left edge, the RIGHT character wins.
        // If the RIGHT character touches the right edge, the LEFT character wins.
        if (heroIsLeft)
        {
            if (heroAtLeft)  { DeclareWin(winnerIsHero: false); return true; }
            if (kingAtRight) { DeclareWin(winnerIsHero: true ); return true; }
        }
        else
        {
            if (kingAtLeft)  { DeclareWin(winnerIsHero: true ); return true; }
            if (heroAtRight) { DeclareWin(winnerIsHero: false); return true; }
        }

        return false;
    }

    void DeclareWin(bool winnerIsHero)
    {
        if (debugLogs) Debug.Log(winnerIsHero ? "[BossMashDuel] HERO WINS!" : "[BossMashDuel] KING WINS!");

        // Lock movement
        enabled = false;

        // Route after a short delay
        StartCoroutine(CoRoute(winnerIsHero));
    }

    IEnumerator CoRoute(bool heroWon)
    {
        yield return new WaitForSeconds(postWinDelay);

        // Prefer EndingRouter if present
        var router = FindFirstObjectByType<EndingRouter>(FindObjectsInactive.Include);
        if (router)
        {
            if (heroWon) 
            {
                EndingRouter.GoToVictory();
            }
            else         
            {
                EndingRouter.GoToGameOver();
            }
            yield break;
        }

        // Fallback: direct load
        if (heroWon) SceneManager.LoadScene("Victory_Neutral");
        else         SceneManager.LoadScene("GameOver");
    }
    
    void CalculateBloodPower()
    {
        int childrenConsumed = GameSession.Instance ? GameSession.Instance.ChildrenKilled : 0;
        
        // Calculate king weakening from blood power
        _bloodPowerWeakening = Mathf.Min(maxBloodWeakening, childrenConsumed * bloodWeakeningPerChild);
        
        // Calculate player strength bonus from blood power
        _bloodPowerStrength = Mathf.Min(maxBloodStrength, childrenConsumed * bloodStrengthPerChild);
        
        if (debugLogs)
        {
            Debug.Log($"[BLOOD POWER] Consumed {childrenConsumed} children | King weakened by {_bloodPowerWeakening:P0} | Player strengthened by {_bloodPowerStrength:P0}");
        }
    }
}
