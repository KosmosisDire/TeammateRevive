using UnityEngine;

namespace TeammateRevive.DeathTotem
{
    public class ScaleAnimation
    {
        private Vector3 targetValue;
        private Vector3 startValue;

        private float duration;
        private float elapsedTime;
        private bool finished;
        
        private Transform target;

        public ScaleAnimation(Transform target, float duration)
        {
            this.target = target;
            this.duration = duration;
        }

        public void AnimateTo(Vector3 targetValue)
        {
            elapsedTime = 0;
            this.targetValue = targetValue;
            finished = false;
            startValue = target.localScale;
        }

        public void Update()
        {
            if (finished) return;
            
            elapsedTime += Time.deltaTime;
            if (elapsedTime >= duration)
            {
                finished = true;
                elapsedTime = duration;
            }

            target.localScale = Vector3.Lerp(startValue, targetValue, elapsedTime / duration);
        }
    }
}