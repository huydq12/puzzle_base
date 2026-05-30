using UnityEngine;
using UnityEngine.SceneManagement;


public class MMBillboard : MonoBehaviour
{
	public Camera MainCamera { get; set; }
	[Tooltip("whether or not this object should automatically grab a camera on start")]
	public bool GrabMainCameraOnStart = true;
	[Tooltip("whether or not to nest this object below a parent container")]
	public bool NestObject = true;
	[Tooltip("the Vector3 to offset the look at direction by")]
	public Vector3 OffsetDirection = Vector3.forward;
	[Tooltip("the Vector3 to consider as 'world up'")]
	public Vector3 Up = Vector3.up;
	protected GameObject _parentContainer;
	private Transform _transform;


	protected virtual void Awake()
	{
		_transform = transform;

		if (GrabMainCameraOnStart == true)
		{
			GrabMainCamera();
		}
	}

	private void Start()
	{
		if (NestObject)
		{
			NestThisObject();
		}
	}

	protected virtual void NestThisObject()
	{
		_parentContainer = new GameObject();
		SceneManager.MoveGameObjectToScene(_parentContainer, this.gameObject.scene);
		_parentContainer.name = "Parent" + transform.gameObject.name;
		_parentContainer.transform.position = transform.position;
		transform.SetParent(_parentContainer.transform);
	}

	protected virtual void GrabMainCamera()
	{
		MainCamera = Camera.main;
	}

	protected virtual void Update()
	{
		if (NestObject)
		{
			_parentContainer.transform.LookAt(_parentContainer.transform.position + MainCamera.transform.rotation * OffsetDirection, MainCamera.transform.rotation * Up);
		}
		else
		{
			_transform.LookAt(_transform.position + MainCamera.transform.rotation * OffsetDirection, MainCamera.transform.rotation * Up);
		}
	}
}
