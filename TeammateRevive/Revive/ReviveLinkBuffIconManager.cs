using RoR2.UI;
using TeammateRevive.Content;
using TeammateRevive.Resources;
using UnityEngine;

namespace TeammateRevive.Revive
{
    public class ReviveLinkBuffIconManager
    {
        private const float MIN_OPACITY = 0f;
        private const float OPACITY_CHANGE_TIME = 1f;
        
        private bool decreasing = false;
        private float elapsedTime = 0;
        
        public ReviveLinkBuffIconManager()
        {
            On.RoR2.UI.BuffIcon.Update += hook_Update;
        }

        private void hook_Update(On.RoR2.UI.BuffIcon.orig_Update orig, BuffIcon buffIcon)
        {
            orig(buffIcon);

            if (!buffIcon.buffDef) return;
            
            var image = buffIcon.iconImage;
            var currentColor = image.color;
            if (buffIcon.buffDef.buffIndex != ReviveLink.Index)
            {
                if (currentColor.a != 1)
                {
                    var clr = currentColor;
                    clr.a = 1;
                    image.color = clr;
                }
                return;
            }
            
            Color start;
            Color target;
            if (this.decreasing)
            {
                start = new Color(currentColor.r, currentColor.g, currentColor.b, 1);
                target = new Color(currentColor.r, currentColor.g, currentColor.b, MIN_OPACITY);
            }
            else
            {
                start = new Color(currentColor.r, currentColor.g, currentColor.b, MIN_OPACITY);
                target = new Color(currentColor.r, currentColor.g, currentColor.b, 1);
            }
            image.color = Color.Lerp(start, target, EaseInCubic(this.elapsedTime / OPACITY_CHANGE_TIME));

            this.elapsedTime += Time.deltaTime;
            while (this.elapsedTime > OPACITY_CHANGE_TIME)
            {
                this.elapsedTime -= OPACITY_CHANGE_TIME;
                this.decreasing = !this.decreasing;
            }
        }

        static float EaseInCubic(float val)
        {
            if (val > 1) return 1;
            return val * val * val;
        }
    }
}