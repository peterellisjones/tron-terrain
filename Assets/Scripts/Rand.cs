using UnityEngine;
using System;

public class Rand
{
	/*
	 * Fast PRNG
	 * 
	 * Returns a random integer
	 * between 0 and 2^31-1 (inclusive)
	 * 
	 * or a random float between 0.0 and 1.0
	 */
	
	private int seed;
	public Rand (int _seed)
	{
		seed = _seed;
	}
	
	public int next(){
		return seed = (1103515245*seed + 12345) & int.MaxValue;
	}
	
	public float nextf(){		
		return (float)next() / (float)int.MaxValue;
	}
}
