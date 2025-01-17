using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class GoblinView : MonoBehaviour
{
    private BaseGoblinModel _currentModel;
    private Dictionary<GoblinState, BaseGoblinModel> _models;
    private GoblinState _newState;
    private GoblinState _currentState;
    private PlayerMovement _player;
    [SerializeField] private MainGameController _mainGameController;
    [SerializeField] private Rigidbody _rigidbody;
    [SerializeField] private Transform _finalAwakeningTransform;
    [SerializeField] private Transform _startAwakeningTransform;
    [SerializeField] private float _smothAwakChangeDistance;

    [SerializeField] private Transform[] _strifePoints;
    [SerializeField] private float _attackCooldown = 2f;
    [SerializeField] private Animator _animator;

    [SerializeField] private int _maxHp = 0;
    [SerializeField] private int _currentHP;

    [SerializeField] private BullEyeView[] _firstPhazeEyes;
    [SerializeField] private BullEyeView[] _secondPhazeEyes;

    [SerializeField] private GameObject _glider;
    private Rigidbody _gliderRigidBody;

    private int _firstPhaseEyesAmount;
    private bool _secondPhase;

    private bool _canBeStucked = false;

    #region ParaThrowBomb
    private Vector3 _fromTo;
    private Vector3 _fromToXZ;
    private float _xMagnitude; //fromToXZ magnitude
    private float _y; //fromto.y
    private float _AngleInRadians;
    private float _TempVelocity;
    #endregion

    public GoblinState State => _currentState;
    public Transform StartAwakTransform => _startAwakeningTransform;
    public Transform FinalAwakTransform => _finalAwakeningTransform;
    public float SmothAwakChangeDistance => _smothAwakChangeDistance;
    public Transform[] StrifePoints => _strifePoints;
    public float AttackCooldown => _attackCooldown;
    public Animator MainAnimator => _animator;
    public PlayerMovement Player => _player;
    private void Awake()
    {
        _newState = GoblinState.Awaiting;
        transform.position = _startAwakeningTransform.position;

        _models = new Dictionary<GoblinState, BaseGoblinModel>();
        _models.Add(GoblinState.Awakening, new AwakeGoblinModel());
        _models.Add(GoblinState.Idle, new IdleGoblinModel());
        _models.Add(GoblinState.Dead, new DeadGoblinModel());
        _models.Add(GoblinState.Awaiting, new AwaitingGoblinModel());
        _currentModel = _models[GoblinState.Awaiting];


        _firstPhaseEyesAmount = 0;
        _secondPhase = false;
        _maxHp = 0;
        foreach (BullEyeView eyes in _firstPhazeEyes)
        {
            eyes.SetGoblinView(this);
            eyes.gameObject.SetActive(false);
            _maxHp++;
            _firstPhaseEyesAmount++;
        }
        foreach (BullEyeView eyes in _secondPhazeEyes)
        {
            eyes.SetGoblinView(this);
            eyes.gameObject.SetActive(false);
            _maxHp++;
        }
        _currentHP = _maxHp;

        _player = FindObjectOfType<PlayerMovement>();
        ChangeState(_newState);
    }

    private void Start()
    {
        GameEvents.Current.OnThrowingBomb += ThrowBomb;
        _mainGameController = FindObjectOfType<MainGameController>();
    }
    private void FixedUpdate()
    {
        if (_newState != _currentState)
        {
            _currentState = _newState;
            _currentModel = _models[_currentState];
        }
        _currentModel.Execute(this);
    }
    public void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag(TagManager.GetTag(TagType.Web)))
        {
            GoblinGetWebHit(collision);
        }
    }
    public void ChangeState(GoblinState state)
    {
        _newState = state;        
        switch (state)
        {
            case GoblinState.Awakening:
                {
                    UpdatePlayerView();
                    break;
                }
            case GoblinState.Dead:
                {
                    _canBeStucked = true;
                    ParticlesController.Current.MakeSmallExplosion(transform.position);
                    _animator.SetTrigger("Stucked");
                    break;
                }
            default: break;
        }

    }

    public void UpdatePlayerView()
    {
        _player = FindObjectOfType<PlayerMovement>();
    }

    public void ThrowBomb(GameObject bomb)
    {
        _AngleInRadians = 45 * Mathf.PI / 180;
        _fromTo = _player.transform.position - bomb.transform.position;
        _fromToXZ = new Vector3(_fromTo.x, 0f, _fromTo.z);

        _xMagnitude = _fromToXZ.magnitude;
        _y = _fromTo.y;

        _TempVelocity = (Physics.gravity.y * _xMagnitude * _xMagnitude) / (2 * (_y - Mathf.Tan(_AngleInRadians) * _xMagnitude) * Mathf.Pow(Mathf.Cos(_AngleInRadians), 2));
        _TempVelocity = Mathf.Sqrt(Mathf.Abs(_TempVelocity));
        //bomb.GetComponent<Rigidbody>().AddForce((_fromToXZ + new Vector3(0, 1, 0)) * _TempVelocity, ForceMode.Impulse);
        bomb.GetComponent<Rigidbody>().velocity = (_fromToXZ.normalized + Vector3.up).normalized * _TempVelocity;

    }

    public void TakeDamage()
    {
        _currentHP--;
        if (_currentHP > 0)
        {
            if (!_secondPhase)
            {
                if (_firstPhaseEyesAmount > 0)
                {
                    _firstPhaseEyesAmount--;
                }
                if (_firstPhaseEyesAmount == 0)
                {
                    EnableSecondPhase();
                }
            }
            else
            {

            }
        }
        if (_currentHP <= 0)
        {
            ChangeState(GoblinState.Dead);
            //_mainGameController.EnemyBeenDefeated(); //<<<--- �� ����� ����
        }
    }

    public void GoblinGetWebHit(Collision collision)//���������� ��� �������� ������� � �������
    {
        if (_canBeStucked) //���������� true, ����� ��� ������ ����������
        {
            _glider.transform.parent = null;
            _gliderRigidBody = _glider.GetComponent<Rigidbody>();
            _gliderRigidBody.isKinematic = false;
            _gliderRigidBody.AddForce((Vector3.left + Vector3.up + Vector3.back) * 10f,ForceMode.Impulse);
            _gliderRigidBody.AddTorque((Vector3.left + Vector3.up + Vector3.back) * 10f, ForceMode.Impulse);
            _glider.GetComponent<MeshCollider>().isTrigger = true;
            GetComponent<BossRagdollController>().WebEnemy(collision);
            Destroy(_glider, 2f);
            Invoke("ExplodeGlider", 1.8f);
        }
    }

    private void ExplodeGlider()
    {
        //������� explode � _glider.position
        ParticlesController.Current.MakeSmallExplosion(_glider.transform.position);
        //�������� �������� �� ��� � ��������

        _mainGameController.EnemyBeenDefeated();
    }

    private void EnableSecondPhase()
    {
        _secondPhase = true;
        foreach (BullEyeView eyes in _secondPhazeEyes)
        {
            eyes.gameObject.SetActive(true);
        }
    }

    public void AwakeGoblin()                           //�������� ��� ��������� ���������
    {
        ChangeState(GoblinState.Awakening);
        foreach (BullEyeView eyes in _firstPhazeEyes)
        {
            eyes.gameObject.SetActive(true);
        }
    }

    public void OnDestroy()
    {
        GameEvents.Current.OnThrowingBomb -= ThrowBomb;

    }

}
