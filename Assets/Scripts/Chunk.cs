using UnityEngine;
using System.Collections;

/*
 * Chunk Manager creates and destroys chunks and the player 
 * moves around
 */

public class Chunk {
	
	private const float GLOBAL_SCALE = 1.0f;
	private int SIZE = 1;       // number of blocks accross (min = 1, max = 128)
	public const float SIZE_RX = GLOBAL_SCALE * 866.0254037844387f; // size in real world units in the x direction
	public const float SIZE_RZ = GLOBAL_SCALE * 1000f; // size in real world units in the z direction
	public const float Y_SCALE = 1f;
	private float BLOCK_SIZE_X;
	private float BLOCK_SIZE_Z;
	private float BLOCK_SIZE_Y; 
	private const int GLOBAL_SEED = 5;
	private const float noisiness = 0.1f;
	private const float up_proclivity = 1.0f;
	private const float down_proclivity = 1.0f;
	private const float not_flat_proclivity = 1.0f;
	private const float corner_proclivity = 0.4f;
	
	public int world_x, world_z;
	public GameObject go;
	MeshFilter mesh_filter;
	MeshRenderer mesh_renderer;
	MeshCollider mesh_collider;
	
	string name;
	
	int[,] heights;

	public Chunk (int _world_x, int _world_z) {
		name = string.Format("Chunk [{0},{1}]",_world_x, _world_z);
		go = new GameObject(name);
		
		mesh_filter = go.AddComponent<MeshFilter>();
		mesh_renderer = go.AddComponent<MeshRenderer>();
		mesh_collider = go.AddComponent<MeshCollider>();	
		
		// initialize properties
		world_x = _world_x;
		world_z = _world_z;
		heights = new int[SIZE+1, SIZE+1];
		
		// need a RNG for each corner
		Rand rand_00 = new Rand((GLOBAL_SEED << 12) ^ ((world_x-1) << 8) ^ ((world_z-1) << 4));
		Rand rand_10 = new Rand((GLOBAL_SEED << 12) ^ (world_x << 8) ^ ((world_z-1) << 4));
		Rand rand_01 = new Rand((GLOBAL_SEED << 12) ^ ((world_x-1) << 8) ^ (world_z << 4));
		Rand rand_11 = new Rand((GLOBAL_SEED << 12) ^ (world_x << 8) ^ (world_z << 4));
		if(rand_00.nextf() < noisiness * corner_proclivity){
			heights[0,0] += (rand_00.next() & 1)*2 - 1;
		}
		if(rand_10.nextf() < noisiness * corner_proclivity){
			heights[SIZE,0] += (rand_10.next() & 1)*2 - 1;
		}
		if(rand_01.nextf() < noisiness * corner_proclivity){
			heights[0,SIZE] += (rand_01.next() & 1)*2 - 1;
		}
		if(rand_11.nextf() < noisiness * corner_proclivity){
			heights[SIZE,SIZE] += (rand_11.next() & 1)*2 - 1;
		}
		Initialize(true);
		// put in correct position
		go.transform.Translate(SIZE_RX * (world_x-0.5f), 0, SIZE_RZ * (world_z-0.5f) + (world_x-0.5f)*0.577350269189626f*SIZE_RX);
		// add collider
		
	}
	
	public Vector3 GetPosition(){
		return go.transform.position;	
	}
	
	public int GetSize(){
		return SIZE;	
	}
	
	public void SetCollider(){
	}
	
	public void DoubleResolution(){
		// doubles the size and recreates the mesh!
		
		int [,] new_heights = new int[SIZE*2+1, SIZE*2+1];
		// interpolate new heights
		for(int x = 0; x < SIZE; x++){
			for(int z = 0; z < SIZE; z++){
				new_heights[x*2,z*2] = heights[x,z]*2;
				new_heights[x*2+1,z*2] = (heights[x,z] + heights[x+1,z]);
				new_heights[x*2,z*2+1] = (heights[x,z] + heights[x,z+1]);
				// center - two rules depending on direction
				new_heights[x*2+1,z*2+1] = (heights[x,z]+heights[x+1,z]+heights[x,z+1]+heights[x+1,z+1])/2;
			}
			// interpolate right and top edges
			new_heights[x*2,SIZE*2] = heights[x,SIZE]*2;
			new_heights[x*2+1,SIZE*2] = (heights[x,SIZE]+heights[x+1,SIZE]);
			new_heights[SIZE*2,x*2] = heights[SIZE,x]*2;
			new_heights[SIZE*2,x*2+1] = (heights[SIZE,x]+heights[SIZE,x+1]);
		}
		new_heights[SIZE*2,SIZE*2] = heights[SIZE,SIZE]*2;
		
		SIZE *= 2;
		heights = new_heights;
		Initialize(true);
	}
	
	public void HalveResolution(){
		int [,] new_heights = new int[(SIZE/2)+1, (SIZE/2)+1];
		for(int x = 0; x < (SIZE/2)+1; x++){
			for(int z = 0; z < (SIZE/2)+1; z++){
				new_heights[x,z] = heights[2*x,2*z];
			}
		}
		SIZE = SIZE/2;
		heights = new_heights;
		Initialize(false);
	}
	
	private void Initialize(bool randomize){	
		BLOCK_SIZE_X = SIZE_RX / SIZE;
		BLOCK_SIZE_Y = Y_SCALE * BLOCK_SIZE_X * 0.86602540378444f; // sqrt(3)/2 or Cosine(30)
		BLOCK_SIZE_Z = SIZE_RZ / SIZE;	
		if(SIZE <= 64 && randomize){
			Randomize();
		}
		// create game object
		SetGameObject();
		// set collider
		//Mesh mesh = go.GetComponent<MeshFilter>().mesh;
		//MeshCollider collider = go.GetComponent<MeshCollider>();	
		//collider.sharedMesh = mesh;
	}
	
	private void Randomize(){
		/*
		 * Check the range of the neighbours heights;
		 * If the neighbours are all the same height or +1, 
		 * then maybe increase height;
		 * Do opposite for decreasing height;
		 */
		// RNG for non-edge points
		Rand inside_rand = new Rand((GLOBAL_SEED << 12) ^ (world_x << 8) ^ (world_z << 4) ^ SIZE); 
		
		/* First lets randomize the edges
		 * we need to a special RNG for this so that other chunks
		 * can generate the same edges
		 */
		Rand left_edge = new Rand((GLOBAL_SEED << 12) ^ ((world_z-1) << 8) ^ (heights[0,0] << 4) ^ (heights[SIZE,0]));
		Rand right_edge = new Rand((GLOBAL_SEED << 12) ^ (world_z << 8) ^ (heights[0,SIZE] << 4) ^ (heights[SIZE,SIZE]));
		Rand top_edge = new Rand((GLOBAL_SEED << 12) ^ (world_x << 8) ^ (heights[SIZE,0] << 4) ^ (heights[SIZE,SIZE]));
		Rand bottom_edge = new Rand((GLOBAL_SEED << 12) ^ (world_x-1 << 8) ^ (heights[0,0] << 4) ^ (heights[0,SIZE]));
		/*
		 * Note that we can only change odd-numbered edge cells
		 * This is because we assume that even-number cells need to line up with
		 * another chunk at half resolution next to this chunk
		 */
		
		// do bottom and top edges
		for(int z = 1; z < SIZE; z+=2){
			// first bottom edge
			int positives = 0;
			int negatives = 0;
			positives += heights[0,z-1]-heights[0,z] >= 1 ? 1 : 0;
			positives += heights[0,z+1]-heights[0,z] >= 1 ? 1 : 0;
			negatives += heights[0,z-1]-heights[0,z] <= 1 ? 1 : 0;
			negatives += heights[0,z+1]-heights[0,z] <= 1 ? 1 : 0;
			// try increasing height
			if(negatives == 0  && positives > 0 && bottom_edge.nextf() < noisiness * up_proclivity){
				heights[0,z] += 1;
			} 
			// try decreasing height
			else if(positives == 0 && negatives > 0 && bottom_edge.nextf() < noisiness * down_proclivity){
				heights[0,z] -= 1;	
			}
			// randomly increase or decrease height on flat terrain
			else if (positives == 0 && negatives == 0 && bottom_edge.nextf() < noisiness * not_flat_proclivity){
				heights[0,z] += (bottom_edge.next() & 1)*2 - 1;
			}
			// now top edge
			positives = 0;
			negatives = 0;
			positives += heights[SIZE,z-1]-heights[SIZE,z] >= 1 ? 1 : 0;
			positives += heights[SIZE,z+1]-heights[SIZE,z] >= 1 ? 1 : 0;
			negatives += heights[SIZE,z-1]-heights[SIZE,z] <= 1 ? 1 : 0;
			negatives += heights[SIZE,z+1]-heights[SIZE,z] <= 1 ? 1 : 0;
			// try increasing height
			if(negatives == 0  && positives > 0 && top_edge.nextf() < noisiness * up_proclivity){
				heights[SIZE,z] += 1;
			} 
			// try decreasing height
			else if(positives == 0 && negatives > 0 && top_edge.nextf() < noisiness * down_proclivity){
				heights[SIZE,z] -= 1;	
			}
			// randomly increase or decrease height on flat terrain
			else if (positives == 0 && negatives == 0 && top_edge.nextf() < noisiness * not_flat_proclivity){
				heights[SIZE,z] += (top_edge.next() & 1)*2 - 1;
			}
		}
		// do left and right edges
		for(int x = 1; x < SIZE; x+=2){
			// first left edge
			int positives = 0;
			int negatives = 0;
			positives += heights[x-1,0]-heights[x,0] >= 1 ? 1 : 0;
			positives += heights[x+1,0]-heights[x,0] >= 1 ? 1 : 0;
			negatives += heights[x-1,0]-heights[x,0] <= 1 ? 1 : 0;
			negatives += heights[x+1,0]-heights[x,0] <= 1 ? 1 : 0;
			// try increasing height
			if(negatives == 0  && positives > 0 && left_edge.nextf() < noisiness * up_proclivity){
				heights[x,0] += 1;
			} 
			// try decreasing height
			else if(positives == 0 && negatives > 0 && left_edge.nextf() < noisiness * down_proclivity){
				heights[x,0] -= 1;	
			}
			// randomly increase or decrease height on flat terrain
			else if (positives == 0 && negatives == 0 && left_edge.nextf() < noisiness * not_flat_proclivity){
				heights[x,0] += (left_edge.next() & 1)*2 - 1;
			}
			// now right edge
			positives = 0;
			negatives = 0;
			positives += heights[x-1,SIZE]-heights[x,SIZE] >= 1 ? 1 : 0;
			positives += heights[x+1,SIZE]-heights[x,SIZE] >= 1 ? 1 : 0;
			negatives += heights[x-1,SIZE]-heights[x,SIZE] <= 1 ? 1 : 0;
			negatives += heights[x+1,SIZE]-heights[x,SIZE] <= 1 ? 1 : 0;
			// try increasing height
			if(negatives == 0  && positives > 0 && right_edge.nextf() < noisiness * up_proclivity){
				heights[x,SIZE] += 1;
			} 
			// try decreasing height
			else if(positives == 0 && negatives > 0 && right_edge.nextf() < noisiness * down_proclivity){
				heights[x,SIZE] -= 1;	
			}
			// randomly increase or decrease height on flat terrain
			else if (positives == 0 && negatives == 0 && right_edge.nextf() < noisiness * not_flat_proclivity){
				heights[x,SIZE] += (right_edge.next() & 1)*2 - 1;
			}
		}
		
		// now do the insides with the inside RNG
		for(int x = 1; x < SIZE; x+=1){
			for(int z = 1; z < SIZE; z+=1){
				// go clockwise starting at bottom left corner of hexagon
				int positives = 0;
				int negatives = 0;
				//
				positives += heights[x-1,z]-heights[x,z] >= 1 ? 1 : 0;
				negatives += heights[x-1,z]-heights[x,z] <= -1 ? 1 : 0;
				//
				positives += heights[x,z-1]-heights[x,z] >= 1 ? 1 : 0;
				negatives += heights[x,z-1]-heights[x,z] <= -1 ? 1 : 0;
				//
				positives += heights[x+1,z-1]-heights[x,z] >= 1 ? 1 : 0;
				negatives += heights[x+1,z-1]-heights[x,z] <= -1 ? 1 : 0;
				//
				positives += heights[x+1,z]-heights[x,z] >= 1 ? 1 : 0;
				negatives += heights[x+1,z]-heights[x,z] <= -1 ? 1 : 0;
				//
				positives += heights[x,z+1]-heights[x,z] >= 1 ? 1 : 0;
				negatives += heights[x,z+1]-heights[x,z] <= -1 ? 1 : 0;
				//
				positives += heights[x-1,z+1]-heights[x,z] >= 1 ? 1 : 0;
				negatives += heights[x-1,z+1]-heights[x,z] <= -1 ? 1 : 0;
	
				// try increasing height
				if(negatives == 0  && positives > 0 && inside_rand.nextf() < noisiness * up_proclivity){
					heights[x,z] += 1;
				} 
				// try decreasing height
				else if(positives == 0 && negatives > 0 && inside_rand.nextf() < noisiness * down_proclivity){
					heights[x,z] -= 1;	
				}
				// randomly increase or decrease height on flat terrain
				else if (positives == 0 && negatives == 0 && inside_rand.nextf() < noisiness * not_flat_proclivity){
					heights[x,z] += (inside_rand.next() & 1)*2 - 1;
				}
			}
		}
	}
	
	public void SetGameObject() {
		
		// remove any existing game object
		Mesh mesh = mesh_filter.mesh;
		GenerateMesh(ref mesh);
		// add texture coordinates:
		Vector2[] uv = new Vector2[(SIZE+3)*(SIZE+3)];
		for(int x = 0; x < SIZE+3; x++){
			int x_idx = x*(SIZE+3);
			for(int z = 0; z < SIZE+3; z++){
				uv[x_idx + z] = new Vector2((x % 2),(z % 2) );
				
			}
		}		
		mesh.uv = uv;
		mesh.RecalculateBounds();
		mesh.RecalculateNormals();
		//Destroy(go.GetComponent<MeshCollider);
		//mesh_collider.sharedMesh = mesh;
		// shade!
		MeshRenderer renderer = go.GetComponent<MeshRenderer>();
		mesh_collider.sharedMesh = mesh_filter.mesh;
		//Shader shader = Resources.Load ("TronShader") as Shader;
		//renderer.material = new Material (shader);
		//renderer.material.color = new Color(0.5f,0.5f,1.0f);
		renderer.material.mainTexture = Resources.Load ("trontext2") as Texture2D;
		
	}
	
	private void Flatten(){
		for(int x = 0; x < SIZE+1; x++){
			for(int z = 0; z < SIZE+1; z++){
				heights[x,z] = 0;		
			}
		}
	}
	
	
	/* We create a mesh that is one column greater on all sides
	 * we then fold that column over to create an 'edge'
	 */
	private void GenerateMesh(ref Mesh mesh){
		// create vertices
		Vector3[] vertices = GenerateVertices();
		mesh.vertices = vertices;
		mesh.triangles = GenerateTriangles(ref vertices);
	}
	
	private Vector3[] GenerateVertices(){
		Vector3[] vertices = new Vector3[(SIZE+3)*(SIZE+3)];
		for(int x = 0; x < SIZE+3; x++){
			int x_idx = x*(SIZE+3);
			for(int z = 0; z < SIZE+3; z++){
				int _x = x-1;
				int _z = z-1;
				if(_x < 0) _x = 0; 
				if(_x >= SIZE+1) _x = SIZE;
				if(_z < 0) _z = 0; 
				if(_z >= SIZE+1) _z = SIZE;
				if(_x != x-1 || _z != z-1){
					vertices[x_idx + z] = new Vector3(_x*BLOCK_SIZE_X, (heights[_x,_z]-1)*BLOCK_SIZE_Y, _z*BLOCK_SIZE_Z + _x*0.577350269189626f*BLOCK_SIZE_X); // 0.577350269189626 = TAN(30)
				} else { 
					vertices[x_idx + z] = new Vector3(_x*BLOCK_SIZE_X, heights[_x,_z]*BLOCK_SIZE_Y, _z*BLOCK_SIZE_Z + _x*0.577350269189626f*BLOCK_SIZE_X); // 0.577350269189626 = TAN(30)
				}
			}
		}		
		return vertices;
	}
	
	private int[] GenerateTriangles(ref Vector3[] vertices){
		// there are SIZE*SIZE squares
		// each square has 2 triangles
		// each triangle has 3 corners...
		int[] triangles = new int[(SIZE+2)*(SIZE+2)*2*3];
		// loop over each square
		for(int x = 0; x < SIZE+2; x++){
			int x_idx = x*(SIZE+3);
			for(int z = 0; z < SIZE+2; z++){
				// calculate the indexes into the vertex 
				// array for the 4 corners of this square
				int v_00 = x_idx + z;
				int v_01 = x_idx + z + 1;
				int v_10 = x_idx + (SIZE+3) + z;
				int v_11 = x_idx + (SIZE+3) + z + 1;
				// starting index into the triangles array
				int t_idx = ((x * (SIZE+2)) + z) * 6;
				// alternate between triangle styles
				//if(z % 2 == 0){
				// lower left triangle
				triangles[t_idx+0] = v_00;
				triangles[t_idx+1] = v_01;
				triangles[t_idx+2] = v_10;
				// upper right triangle
				triangles[t_idx+3] = v_10;
				triangles[t_idx+4] = v_01;
				triangles[t_idx+5] = v_11;
			}
		}	
		return triangles;
	}
	
}