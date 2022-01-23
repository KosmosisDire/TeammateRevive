using UnityEngine;

namespace TeammateRevive.Skull
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
            this.elapsedTime = 0;
            this.targetValue = targetValue;
            this.finished = false;
            this.startValue = this.target.localScale;
        }

        public void Update()
        {
            if (this.finished) return;
            
            this.elapsedTime += Time.deltaTime;
            if (this.elapsedTime >= this.duration)
            {
                this.finished = true;
                this.elapsedTime = this.duration;
            }

            this.target.localScale = Vector3.Lerp(this.startValue, this.targetValue, this.elapsedTime / this.duration);
        }
    }
}