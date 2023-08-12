using NewHorizons.Utility;
using NewHorizons.Utility.OWML;
using UnityEngine;

namespace NewHorizons.Components.SizeControllers
{
    public class CometTailController : SizeController
    {
        private Transform _dustTargetBody;
        private OWRigidbody _primaryBody;
        private OWRigidbody _body;

        private bool _hasRotationOverride;
        private bool _hasPrimaryBody;

        public GameObject gasTail;
        public GameObject dustTail;

        private Vector3 _gasTarget;
        private Vector3 _dustTarget;

        public void Start()
        {
            _body = transform.GetAttachedOWRigidbody();
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            if (!_hasRotationOverride && _hasPrimaryBody)
            {
                UpdateTargetPositions();

                dustTail?.LookDir(_dustTarget);
                gasTail?.LookDir(_gasTarget);
            }
        }

        private void UpdateTargetPositions()
        {
            // body is null for proxies
            // TODO: this will make proxy tails face the real body rather than proxy body (ie wrong). fix properly in a different PR
            var toPrimary = ((_body ? _body.transform : transform).position - _dustTargetBody.transform.position).normalized;
            var velocityDirection = (_primaryBody?.GetVelocity() ?? Vector3.zero) - (_body ? _body.GetVelocity() : Vector3.zero); // Accept that this is flipped ok

            var tangentVel = Vector3.ProjectOnPlane(velocityDirection, toPrimary) / velocityDirection.magnitude;

            _gasTarget = toPrimary;
            _dustTarget = (toPrimary + tangentVel).normalized;
        }

        public void SetRotationOverride(Vector3 eulerAngles)
        {
            _hasRotationOverride = true;
            transform.localRotation = Quaternion.Euler(eulerAngles);
        }

        public void SetPrimaryBody(Transform dustTarget, OWRigidbody primaryBody)
        {
            _hasPrimaryBody = true;
            _dustTargetBody = dustTarget;
            _primaryBody = primaryBody;
        }
    }
}
