using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Prototype
{
    public class StackPrototypeController : MonoBehaviour
    {
        private const float CUT_EPSILON = 0.0001f;
        private const float PERFECT_THRESHOLD = 0.05f;
        private const float PERFECT_SIZE_RATIO = 0.5f;
        private const int RESTORE_COMBO_INTERVAL = 2;
        private const int BONUS_INTERVAL = 5;
        private const float MIN_BONUS_SIZE = 0.4f;
        private const float BONUS_OFFSET_RATIO = 0.25f;
        private const float BONUS_MAX_TOLERANCE = 0.04f;
        private const float BONUS_TOLERANCE_RATIO = 0.05f;
        private const float BONUS_MIN_OVERLAP_RATIO = 0.65f;

        [SerializeField] private StackCameraController _cameraController;

        [SerializeField] private AudioSource _perfectAudioSource;
        [SerializeField] private AudioClip _perfectClip;
        [SerializeField] private GameObject _blockPrefab;
        [SerializeField] private GameObject _bonusMarkerPrefab;
        [SerializeField] private InputActionReference _placeActionReference;
        [SerializeField] private Vector3 _initialSize = new Vector3(3f, 0.4f, 3f);
        [SerializeField] private float _moveRange = 3.5f;
        [SerializeField] private float _moveSpeed = 2.5f;
        [SerializeField] private float _restoreAmount = 0.15f;

        private List<GameObject> _spawnedBlocks = new List<GameObject>();
        private GameObject _topBlock;
        private GameObject _movingBlock;
        private float _moveTime;
        private int _placedCount;
        private bool _moveOnX;
        private bool _isGameOver;
        private int _perfectCombo;
        private GameObject _bonusMarker;
        private float _bonusTargetCenter;
        private float _bonusCollectTolerance;
        private bool _nextBonusPositiveSide = true;

        public event Action<int> PerfectActioned;
        public event Action BonusCollectedActioned;

        public Transform TopBlockTransform => _topBlock == null ? null : _topBlock.transform;

        private void OnEnable()
        {
            if ( _placeActionReference == null || _placeActionReference.action == null )
            {
                Debug.LogError("Place Action Reference를 연결해 주세요.");
                enabled = false;
                return;
            }

            _placeActionReference.action.Enable();
        }

        private void OnDisable()
        {
            if ( _placeActionReference != null && _placeActionReference.action != null )
            {
                _placeActionReference.action.Disable();
            }
        }

        private void Start()
        {
            if ( _blockPrefab == null )
            {
                Debug.LogError("Block Prefab을 연결해 주세요.");
                enabled = false;
                return;
            }

            RestartGame();
        }

        private void Update()
        {
            bool placePressed = _placeActionReference.action.WasPressedThisFrame();

            if ( _isGameOver )
            {
                if ( placePressed )
                {
                    RestartGame();
                }

                return;
            }

            MoveBlock();

            if ( placePressed )
            {
                PlaceBlock();
            }
        }

        private void RestartGame()
        {
            ClearBonusMarker();

            foreach ( GameObject block in _spawnedBlocks )
            {
                if ( block != null )
                {
                    Destroy(block);
                }
            }

            _spawnedBlocks.Clear();
            _placedCount = 0;
            _perfectCombo = 0;
            _moveOnX = true;
            _isGameOver = false;
            _nextBonusPositiveSide = true;

            _topBlock = CreateBlock(Vector3.zero , _initialSize);
            SpawnMovingBlock();

            if ( _cameraController != null )
            {
                _cameraController.SnapToTop();
            }
        }

        private GameObject CreateBlock(Vector3 position , Vector3 size)
        {
            GameObject block = Instantiate(_blockPrefab, position, Quaternion.identity);
            block.transform.localScale = size;
            _spawnedBlocks.Add(block);
            return block;
        }

        private void SpawnMovingBlock()
        {
            Vector3 position = _topBlock.transform.position;
            position.y += _topBlock.transform.localScale.y;

            if ( _moveOnX )
            {
                position.x -= _moveRange;
            }
            else
            {
                position.z -= _moveRange;
            }

            _movingBlock = CreateBlock(position , _topBlock.transform.localScale);
            _moveTime = 0f;
            TrySpawnBonusMarker();
        }

        private void TrySpawnBonusMarker()
        {
            if ( _placedCount == 0 || _placedCount % BONUS_INTERVAL != 0 )
            {
                return;
            }

            float previousSize = _moveOnX
                ? _topBlock.transform.localScale.x
                : _topBlock.transform.localScale.z;

            if ( previousSize < MIN_BONUS_SIZE )
            {
                return;
            }

            float perfectThreshold = Mathf.Min(PERFECT_THRESHOLD , previousSize * PERFECT_SIZE_RATIO);
            float centerOffset = previousSize * BONUS_OFFSET_RATIO;
            float collectTolerance = Mathf.Min(BONUS_MAX_TOLERANCE , previousSize * BONUS_TOLERANCE_RATIO);

            if ( centerOffset - collectTolerance <= perfectThreshold ||
                 centerOffset + collectTolerance > previousSize * (1f - BONUS_MIN_OVERLAP_RATIO) )
            {
                return;
            }

            float previousCenter = _moveOnX
                ? _topBlock.transform.position.x
                : _topBlock.transform.position.z;

            _bonusTargetCenter = previousCenter + (_nextBonusPositiveSide ? centerOffset : -centerOffset);
            _bonusCollectTolerance = collectTolerance;

            Vector3 markerPosition = _movingBlock.transform.position;
            if ( _moveOnX )
            {
                markerPosition.x = _bonusTargetCenter;
            }
            else
            {
                markerPosition.z = _bonusTargetCenter;
            }

            markerPosition.y += _movingBlock.transform.localScale.y * 0.5f + 0.2f;
            CreateBonusMarker(markerPosition);
            _nextBonusPositiveSide = !_nextBonusPositiveSide;

            Debug.Log($"BONUS SPAWN: {_placedCount + 1}번째 블록");
        }

        private void CreateBonusMarker(Vector3 markerPosition)
        {
            if ( _bonusMarkerPrefab != null )
            {
                _bonusMarker = Instantiate(_bonusMarkerPrefab , markerPosition , Quaternion.identity);
            }
            else
            {
                _bonusMarker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                _bonusMarker.name = "BonusMarker";
                _bonusMarker.transform.position = markerPosition;
                _bonusMarker.transform.localScale = new Vector3(
                    _bonusCollectTolerance * 2f , 0.15f , _bonusCollectTolerance * 2f);

                Renderer markerRenderer = _bonusMarker.GetComponent<Renderer>();
                MaterialPropertyBlock properties = new MaterialPropertyBlock();
                properties.SetColor("_BaseColor" , Color.yellow);
                properties.SetColor("_Color" , Color.yellow);
                markerRenderer.SetPropertyBlock(properties);
            }

            foreach ( Collider markerCollider in _bonusMarker.GetComponentsInChildren<Collider>() )
            {
                markerCollider.enabled = false;
            }
        }

        private void ClearBonusMarker()
        {
            if ( _bonusMarker == null )
            {
                return;
            }

            _bonusMarker.SetActive(false);
            Destroy(_bonusMarker);
            _bonusMarker = null;
        }

        private void MoveBlock()
        {
            _moveTime += Time.deltaTime;

            float offset = Mathf.PingPong(
            _moveTime * _moveSpeed,
            _moveRange * 2f
        ) - _moveRange;

            Vector3 position = _movingBlock.transform.position;
            Vector3 topPosition = _topBlock.transform.position;

            if ( _moveOnX )
            {
                position.x = topPosition.x + offset;
            }
            else
            {
                position.z = topPosition.z + offset;
            }

            _movingBlock.transform.position = position;
        }

        private void PlaceBlock()
        {
            Vector3 previousPosition = _topBlock.transform.position;
            Vector3 currentPosition = _movingBlock.transform.position;
            Vector3 currentSize = _movingBlock.transform.localScale;

            float previousCenter = _moveOnX
            ? previousPosition.x
            : previousPosition.z;

            float currentCenter = _moveOnX
            ? currentPosition.x
            : currentPosition.z;

            float previousSize = _moveOnX
            ? _topBlock.transform.localScale.x
            : _topBlock.transform.localScale.z;

            float movingSize = _moveOnX
            ? currentSize.x
            : currentSize.z;

            float previousLeft = previousCenter - previousSize / 2f;
            float previousRight = previousCenter + previousSize / 2f;
            float currentLeft = currentCenter - movingSize / 2f;
            float currentRight = currentCenter + movingSize / 2f;

            float overlapLeft = Mathf.Max(previousLeft, currentLeft);
            float overlapRight = Mathf.Min(previousRight, currentRight);
            float overlapSize = overlapRight - overlapLeft;
            bool bonusTargetHit = _bonusMarker != null &&
                Mathf.Abs(currentCenter - _bonusTargetCenter) <= _bonusCollectTolerance;

            ClearBonusMarker();

            if ( overlapSize <= 0f )
            {
                _perfectCombo = 0;
                _movingBlock.AddComponent<Rigidbody>();
                _isGameOver = true;
                Debug.Log($"Game Over - 쌓은 블록: {_placedCount}");
                return;
            }

            float perfectThreshold = Mathf.Min(
    PERFECT_THRESHOLD,
    previousSize * PERFECT_SIZE_RATIO
);

            if ( Mathf.Abs(currentCenter - previousCenter) <= perfectThreshold )
            {
                currentPosition.x = previousPosition.x;
                currentPosition.z = previousPosition.z;
                _movingBlock.transform.position = currentPosition;

                _perfectCombo++;
                _topBlock = _movingBlock;
                _placedCount++;
                _moveOnX = !_moveOnX;

                if ( _perfectCombo % RESTORE_COMBO_INTERVAL == 0 )
                {
                    RestoreTopBlock();
                }

                Debug.Log($"PERFECT x{_perfectCombo}");
                PerfectActioned?.Invoke(_perfectCombo);

                if ( _perfectAudioSource != null && _perfectClip != null )
                {
                    _perfectAudioSource.PlayOneShot(_perfectClip);
                }

                SpawnMovingBlock();
                return;
            }

            _perfectCombo = 0;

            float remainingCenter = (overlapLeft + overlapRight) / 2f;
            float cutSize = movingSize - overlapSize;

            if ( cutSize > CUT_EPSILON )
            {
                float cutCenter = currentCenter > previousCenter
                ? (overlapRight + currentRight) / 2f
                : (currentLeft + overlapLeft) / 2f;

                CreateCutBlock(cutCenter , cutSize);
            }
            else
            {
                remainingCenter = previousCenter;
                overlapSize = movingSize;
            }

            if ( _moveOnX )
            {
                currentPosition.x = remainingCenter;
                currentSize.x = overlapSize;
            }
            else
            {
                currentPosition.z = remainingCenter;
                currentSize.z = overlapSize;
            }

            _movingBlock.transform.position = currentPosition;
            _movingBlock.transform.localScale = currentSize;

            _topBlock = _movingBlock;
            _placedCount++;
            _moveOnX = !_moveOnX;

            if ( bonusTargetHit )
            {
                Debug.Log($"BONUS COLLECTED: {_placedCount}번째 블록");
                BonusCollectedActioned?.Invoke();
            }

            Debug.Log($"Placed: {_placedCount}, Size: {currentSize}");
            SpawnMovingBlock();
        }

        private void RestoreTopBlock()
        {
            Vector3 currentSize = _topBlock.transform.localScale;
            Vector3 restoredSize = currentSize;

            restoredSize.x = Mathf.Min(currentSize.x + _restoreAmount , _initialSize.x);
            restoredSize.z = Mathf.Min(currentSize.z + _restoreAmount , _initialSize.z);

            if ( restoredSize.x <= currentSize.x && restoredSize.z <= currentSize.z )
            {
                return;
            }

            _topBlock.transform.localScale = restoredSize;
            Debug.Log($"RESTORE x{_perfectCombo}: {restoredSize}");
        }

        private void CreateCutBlock(float cutCenter , float cutSize)
        {
            Vector3 position = _movingBlock.transform.position;
            Vector3 size = _movingBlock.transform.localScale;

            if ( _moveOnX )
            {
                position.x = cutCenter;
                size.x = cutSize;
            }
            else
            {
                position.z = cutCenter;
                size.z = cutSize;
            }

            GameObject cutBlock = CreateBlock(position, size);
            cutBlock.AddComponent<Rigidbody>();
            Destroy(cutBlock , 3f);
        }
    }
}

