using UnityEngine;

namespace Resonance.Train
{
    [RequireComponent(typeof(CharacterController))]
    public class TrainPassengerPhysics : MonoBehaviour
    {
        [Header("Train Reference")]
        [SerializeField] private TrainController _trainController;

        [Header("Boarding Detection")]
        [SerializeField] private string _trainFloorTag = "Train";

        [Header("Inertia")]
        [SerializeField] private float _inertiaDecay = 4f;
        [SerializeField] private float _maxInertiaSpeed = 18f;

        public bool IsOnTrain { get; private set; }

        public Vector3 GetFrameVelocityOffset() => _frameOffset;

        public void ClearInertia()
        {
            _inertiaVelocity = Vector3.zero;
            _frameOffset = Vector3.zero;
        }

        private CharacterController _characterController;
        private Vector3 _frameOffset = Vector3.zero;
        private Vector3 _inertiaVelocity = Vector3.zero;
        private bool _wasOnTrainLastFrame = false;

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();

            if (_trainController == null)
                _trainController = FindFirstObjectByType<TrainController>();
        }

        private void FixedUpdate()
        {
            UpdateBoardingState();
            ComputeFrameOffset();
        }

        private void UpdateBoardingState()
        {
            Vector3 feetPos = transform.position + _characterController.center - Vector3.up * (_characterController.height * 0.5f - _characterController.radius);
            Collider[] hits = Physics.OverlapSphere(feetPos, _characterController.radius + 0.05f);

            bool onTrain = false;
            foreach (var collider in hits)
            {
                if (collider.CompareTag(_trainFloorTag))
                {
                    onTrain = true;
                    break;
                }
            }

            if (_wasOnTrainLastFrame && !onTrain && _trainController != null)
            {
                Vector3 trainVelocity = _trainController.Velocity;
                trainVelocity.y = 0f;
                _inertiaVelocity = Vector3.ClampMagnitude(trainVelocity, _maxInertiaSpeed);
            }

            IsOnTrain = onTrain;
            _wasOnTrainLastFrame = onTrain;
        }

        private void ComputeFrameOffset()
        {
            _frameOffset = Vector3.zero;

            if (IsOnTrain && _trainController != null)
            {
                Vector3 trainVelocity = _trainController.Velocity;
                trainVelocity.y = 0f;
                _frameOffset = trainVelocity;
            }
            else if (_inertiaVelocity.sqrMagnitude > 0.001f)
            {
                _frameOffset = _inertiaVelocity;
                _inertiaVelocity = Vector3.MoveTowards(_inertiaVelocity, Vector3.zero, _inertiaDecay * Time.fixedDeltaTime);
            }
        }
    }
}