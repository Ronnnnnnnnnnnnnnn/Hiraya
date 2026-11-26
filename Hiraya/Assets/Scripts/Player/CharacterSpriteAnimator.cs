using UnityEngine;

namespace Hiraya
{
    /// <summary>
    /// Swaps the character's sprite: stand sprite when idle, a walk cycle
    /// (walk 1, stand, walk 2, stand) while moving. Picks Fin or Fiona from the Main Menu choice.
    /// </summary>
    public class CharacterSpriteAnimator : MonoBehaviour
    {
        [SerializeField] PlayerController player;
        [SerializeField] SpriteRenderer spriteRenderer;

        [Header("Characters")]
        [SerializeField] CharacterSkin fin;
        [SerializeField] CharacterSkin fiona;

        [Header("Animation")]
        [SerializeField] float stepsPerSecond = 8f;

        CharacterSkin skin;
        float timer;
        int step;

        void Awake()
        {
            skin = GameSession.Character == CharacterChoice.Fiona ? fiona : fin;
            if (skin == null) skin = fin != null ? fin : fiona;
        }

        void Start() => Apply();

        void LateUpdate()
        {
            if (player.IsMoving)
            {
                timer += Time.deltaTime;
                float stepTime = 1f / Mathf.Max(0.1f, stepsPerSecond);
                while (timer >= stepTime)
                {
                    timer -= stepTime;
                    step = (step + 1) % 4;
                }
            }
            else
            {
                timer = 0f;
                step = 0;
            }

            Apply();
        }

        void Apply()
        {
            if (skin == null || spriteRenderer == null) return;

            Sprite sprite;
            if (!player.IsMoving)
            {
                sprite = skin.Stand(player.Facing);
            }
            else
            {
                // step 0: walk 1, step 1: stand, step 2: walk 2, step 3: stand
                switch (step)
                {
                    case 0: sprite = skin.Walk(player.Facing, 0); break;
                    case 2: sprite = skin.Walk(player.Facing, 1); break;
                    default: sprite = skin.Stand(player.Facing); break;
                }
            }

            if (sprite != null) spriteRenderer.sprite = sprite;
        }
    }
}
