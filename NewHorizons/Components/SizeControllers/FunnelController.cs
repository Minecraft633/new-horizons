using UnityEngine;
namespace NewHorizons.Components.SizeControllers
{
    public class FunnelController : SizeController
    {
        public Transform target;
        public Transform anchor;

        public override void FixedUpdate()
        {
            // Temporary solution that i will never get rid of
            transform.position = anchor.position;

            UpdateScale();

            float num = CurrentScale;

            var dist = (transform.position - target.position).magnitude;
            transform.localScale = new Vector3(num, num, dist / 500f);

            transform.LookAt(target);

            // The target or anchor could have been destroyed by a star
            if (!target.gameObject.activeInHierarchy || !anchor.gameObject.activeInHierarchy)
            {
                gameObject.SetActive(false);
            }
        }
    }
}
