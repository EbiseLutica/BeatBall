using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[ExecuteInEditMode]
public class LongNoteMeshModifier : MonoBehaviour {

	MeshFilter meshFilter;

	[SerializeField]
	private Vector3 endPosition;

	public Vector3 EndPosition
	{
		get { return endPosition; }
		set { endPosition = value; }
	}

	[SerializeField]
	private float width;

	public float Width
	{
		get { return width; }
		set { width = value; }
	}

	private Vector3 prevEndPos;
	private float prevWidth;
	private Vector3 prevPos;

	// Use this for initialization
	void Start () {
		meshFilter = GetComponent<MeshFilter>();
	}
	
	// Update is called once per frame
	void Update () {
		if (IsParameterUpdated)
			meshFilter.mesh = MakeMesh();
	}

	bool IsParameterUpdated
	{
		get
		{
			var ret = prevWidth.Equals(Width) && prevEndPos.Equals(EndPosition) && prevPos.Equals(transform.position);
			prevWidth = Width;
			prevEndPos = EndPosition;
			prevPos = transform.position;
			return ret;
		}
	}

	Mesh MakeMesh()
	{
		var mesh = new Mesh();

		var your = endPosition;

		var r = Width / 2;

		mesh.SetVertices(new List<Vector3>
		{
			new Vector3(your.x - r, your.y, -your.z), // LD
			new Vector3(your.x + r, your.y, -your.z), // RD
			new Vector3(-r, 0, 0), // LU
			new Vector3(r, 0, 0), // RU
		});

		mesh.SetUVs(0, new List<Vector2>
		{
			new Vector2(0, 0),
			new Vector2(1, 0),
			new Vector2(0, 1),
			new Vector2(1, 1),
		});

		mesh.SetIndices(new[] { 0, 2, 1, 1, 2, 3 }, MeshTopology.Triangles, 0);

		mesh.name = "(Auto Generated Mesh)";

		return mesh;
	}
}
