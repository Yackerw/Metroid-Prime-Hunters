using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine;

public class Utilj {
    /// <summary>
    /// Returns a FileStream handle to the file, or NULL if file failed to load. Useful to prevent errors that can normally happen with File.Open
    /// </summary>
    /// <param name="name"></param>
    /// <param name="fm"></param>
    /// <param name="fa"></param>
    /// <returns></returns>
    static public FileStream FileOpen(string name, FileMode fm, FileAccess fa)
    {
        FileStream fs = null;
        if (!File.Exists(name) && fm != FileMode.Create && fm != FileMode.CreateNew && fm != FileMode.OpenOrCreate)
        {
            return null;
        }
        try
        {
            if (fa == FileAccess.Read)
            {
                fs = File.Open(name, fm, fa, FileShare.ReadWrite);
            } else
            {
                fs = File.Open(name, fm, fa, FileShare.Read);
            }
        }
        catch
        {
            return null;
        }
        return fs;
    }

    /// <summary>
    /// Returns a string from within an array.
    /// </summary>
    /// <param name="array"></param>
    /// <param name="offset"></param>
    /// <param name="string"></param>
    /// <returns></returns>
    public static int StringFromArray(byte[] array, int offset, out string str)
    {
        str = "";
        while (offset < array.Length && array[offset] != 0)
        {
            str = string.Concat(str, Convert.ToChar(array[offset]));
            ++offset;
        }
        return offset + 1;
    }

    /// <summary>
    /// Takes 4 verts, and makes a double sided quad out of them.
    /// </summary>
    /// <param name="verts"></param>
    /// <returns></returns>
    static public int[] MakeQuad(int[] verts)
    {
        int[] tris = new int[12];
        // triangle one
        tris[0] = verts[0];
        tris[1] = verts[2];
        tris[2] = verts[1];
        // triangle two
        tris[3] = verts[2];
        tris[4] = verts[3];
        tris[5] = verts[1];
        // triangle three
        tris[6] = verts[1];
        tris[7] = verts[2];
        tris[8] = verts[0];
        // triangle four
        tris[9] = verts[1];
        tris[10] = verts[3];
        tris[11] = verts[2];
        return tris;
    }

    static public void AddToArray(int val, byte[] arr, int offset)
    {
        Array.Copy(BitConverter.GetBytes(val), 0, arr, offset, 4);
    }

    static public void AddToArray(float val, byte[] arr, int offset)
    {
        Array.Copy(BitConverter.GetBytes(val), 0, arr, offset, 4);
    }

    static public void AddToArray(Vector3 val, byte[] arr, int offset)
    {
        Array.Copy(BitConverter.GetBytes(val.x), 0, arr, offset, 4);
        Array.Copy(BitConverter.GetBytes(val.y), 0, arr, offset + 4, 4);
        Array.Copy(BitConverter.GetBytes(val.z), 0, arr, offset + 8, 4);
    }

    static public void AddToArray(Quaternion val, byte[] arr, int offset)
    {
        Array.Copy(BitConverter.GetBytes(val.x), 0, arr, offset, 4);
        Array.Copy(BitConverter.GetBytes(val.y), 0, arr, offset + 4, 4);
        Array.Copy(BitConverter.GetBytes(val.z), 0, arr, offset + 8, 4);
        Array.Copy(BitConverter.GetBytes(val.w), 0, arr, offset + 12, 4);
    }

    static public void AddToArray(bool val, byte[] arr, int offset)
    {
        arr[offset] = BitConverter.GetBytes(val)[0];
    }

	static public void AddToArray(short val, byte[] arr, int offset)
	{
		Array.Copy(BitConverter.GetBytes(val), 0, arr, offset, 2);
	}

    static public byte[] StringToArray(string str)
    {
        byte[] retval = System.Text.Encoding.UTF8.GetBytes(str);
        Array.Resize(ref retval, retval.Length + 1);
        return retval;
    }

    static public Vector3 ReadVector3Array(byte[] arr, int offset)
    {
        if (arr.Length < offset + 12) return new Vector3();
        return new Vector3(BitConverter.ToSingle(arr, offset), BitConverter.ToSingle(arr, offset + 4), BitConverter.ToSingle(arr, offset + 8));
    }

    static public Quaternion ReadQuaternionArray(byte[] arr, int offset)
    {
        if (arr.Length < offset + 16) return new Quaternion();
        return new Quaternion(BitConverter.ToSingle(arr, offset), BitConverter.ToSingle(arr, offset + 4), BitConverter.ToSingle(arr, offset + 8), BitConverter.ToSingle(arr, offset + 12));
    }

    static public String StringFromFile(FileStream fs)
    {
        String ret = "";
        byte[] chr = new byte[1];
        fs.Read(chr, 0, 1);
        while (chr[0] != 0)
        {
            ret = String.Concat(ret, Convert.ToChar(chr[0]));
            fs.Read(chr, 0, 1);
        }
        return ret;
    }

	static List<Vector3> combiVerts = new List<Vector3>();
	static List<int> combiTris = new List<int>();
	static List<Vector2> combiUVs = new List<Vector2>();
	static List<Color> combiColors = new List<Color>();

	/// <summary>
	/// Mesh.CombineMeshes except it actually works
	/// note: doesn't account for submeshes
	/// </summary>
	/// <param name="meshes"></param>
	/// <param name="transforms"></param>
	/// <returns></returns>
	static public Mesh CombineMeshes(Mesh[] meshes, List<Transform> transforms)
	{
		if (meshes.Length < transforms.Count)
		{
			return new Mesh();
		}
		// attempt to combine all
		Mesh m2 = new Mesh();
		int offs = 0;
		// holy shit this thing is gonna destroy gc
		for (int i = 0; i < transforms.Count; ++i)
		{
			// add our verts
			Vector3[] newVerts = meshes[i].vertices;
			for (int i2 = 0; i2 < newVerts.Length; ++i2)
			{
				newVerts[i2] = transforms[i].TransformPoint(newVerts[i2]);
			}
			combiVerts.AddRange(newVerts);
			combiUVs.AddRange(meshes[i].uv);
			combiColors.AddRange(meshes[i].colors);
			// add our tris
			int[] newTris = meshes[i].GetTriangles(0);
			for (int i2 = 0; i2 < newTris.Length; ++i2)
			{
				newTris[i2] += offs;
			}
			combiTris.AddRange(newTris);
			//update offset
			offs = combiVerts.Count;

			// account for lack of UVs
			while (combiUVs.Count < combiVerts.Count)
			{
				combiUVs.Add(Vector2.zero);
			}
		}
		m2.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
		m2.SetVertices(combiVerts);
		m2.SetUVs(0, combiUVs);
		m2.SetColors(combiColors);
		m2.subMeshCount = 1;
		m2.SetTriangles(combiTris, 0);
		m2.RecalculateNormals();
		combiTris.Clear();
		combiUVs.Clear();
		combiVerts.Clear();
		combiColors.Clear();
		return m2;
	}

	/// <summary>
	/// Same as CombineMeshes, but it takes a position list and quaternion instead
	/// </summary>
	/// <param name="meshes"></param>
	/// <param name="poses"></param>
	/// <param name="rot"></param>
	/// <returns></returns>
	static public Mesh CombineMeshesNoTrans(Mesh[] meshes, List<Vector3> poses, Quaternion rot)
	{
		if (meshes.Length < poses.Count)
		{
			return new Mesh();
		}
		// attempt to combine all
		Mesh m2 = new Mesh();
		int offs = 0;
		// holy shit this thing is gonna destroy gc
		for (int i = 0; i < poses.Count; ++i)
		{
			// add our verts
			Vector3[] newVerts = meshes[i].vertices;
			for (int i2 = 0; i2 < newVerts.Length; ++i2)
			{
				newVerts[i2] = rot * newVerts[i2];
				newVerts[i2] += poses[i];
			}
			combiVerts.AddRange(newVerts);
			combiUVs.AddRange(meshes[i].uv);
			combiColors.AddRange(meshes[i].colors);
			// add our tris
			int[] newTris = meshes[i].GetTriangles(0);
			for (int i2 = 0; i2 < newTris.Length; ++i2)
			{
				newTris[i2] += offs;
			}
			combiTris.AddRange(newTris);
			//update offset
			offs = combiVerts.Count;

			// account for lack of UVs
			while (combiUVs.Count < combiVerts.Count)
			{
				combiUVs.Add(Vector2.zero);
			}
		}
		m2.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
		m2.SetVertices(combiVerts);
		m2.SetUVs(0, combiUVs);
		m2.SetColors(combiColors);
		m2.subMeshCount = 1;
		m2.SetTriangles(combiTris, 0);
		m2.RecalculateNormals();
		combiTris.Clear();
		combiUVs.Clear();
		combiVerts.Clear();
		combiColors.Clear();
		return m2;
	}

	static public void ClearMeshCache()
	{
		combiVerts = new List<Vector3>();
		combiTris = new List<int>();
		combiUVs = new List<Vector2>();
		combiColors = new List<Color>();
	}
}
