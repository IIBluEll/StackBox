using UnityEngine;

namespace Prototype
{
    public class StackCameraController : MonoBehaviour
    {
        [SerializeField] private StackPrototypeController _stackPrototypeController;
        [SerializeField]
        private Vector3 _followOffset =
            new Vector3(6f, 6f, -8f);
        [SerializeField] private float _lookAheadHeight = 1f;
        [SerializeField] private float _smoothTime = 0.25f;

        private Vector3 _velocity;

        public void SnapToTop()
        {
            if ( _stackPrototypeController == null )
            {
                return;
            }

            Transform topBlockTrans =
                _stackPrototypeController.TopBlockTransform;

            if ( topBlockTrans == null )
            {
                return;
            }

            Vector3 anchor =
                topBlockTrans.position + Vector3.up * _lookAheadHeight;

            transform.position = anchor + _followOffset;
            transform.rotation =
                Quaternion.LookRotation(-_followOffset , Vector3.up);

            _velocity = Vector3.zero;
        }

        private void LateUpdate()
        {
            if ( _stackPrototypeController == null )
            {
                return;
            }

            Transform topBlockTrans =
                _stackPrototypeController.TopBlockTransform;

            if ( topBlockTrans == null )
            {
                return;
            }

            Vector3 anchor =
                topBlockTrans.position + Vector3.up * _lookAheadHeight;

            Vector3 desiredPosition = anchor + _followOffset;

            transform.position = Vector3.SmoothDamp(
                transform.position ,
                desiredPosition ,
                ref _velocity ,
                _smoothTime
            );

            transform.rotation =
                Quaternion.LookRotation(-_followOffset , Vector3.up);
        }
    }
}

