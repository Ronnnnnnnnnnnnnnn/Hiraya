using UnityEngine;

namespace Hiraya
{
    /// <summary>All the sprites of one character (Fin or Fiona): standing + two walk frames for each direction.</summary>
    [CreateAssetMenu(menuName = "Hiraya/Character Skin", fileName = "CharacterSkin")]
    public class CharacterSkin : ScriptableObject
    {
        public string displayName;

        [Header("Standing")]
        public Sprite front;
        public Sprite back;
        public Sprite left;
        public Sprite right;

        [Header("Walking - front (toward camera)")]
        public Sprite walkFront1;
        public Sprite walkFront2;

        [Header("Walking - back (away from camera)")]
        public Sprite walkBack1;
        public Sprite walkBack2;

        [Header("Walking - left")]
        public Sprite walkLeft1;
        public Sprite walkLeft2;

        [Header("Walking - right")]
        public Sprite walkRight1;
        public Sprite walkRight2;

        public Sprite Stand(Facing facing)
        {
            switch (facing)
            {
                case Facing.Up: return back;
                case Facing.Left: return left;
                case Facing.Right: return right;
                default: return front;
            }
        }

        /// <param name="frame">0 or 1</param>
        public Sprite Walk(Facing facing, int frame)
        {
            bool first = frame == 0;
            switch (facing)
            {
                case Facing.Up: return first ? walkBack1 : walkBack2;
                case Facing.Left: return first ? walkLeft1 : walkLeft2;
                case Facing.Right: return first ? walkRight1 : walkRight2;
                default: return first ? walkFront1 : walkFront2;
            }
        }
    }
}
