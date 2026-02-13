namespace QuadTree
{
    using UnityEngine;
    using System.Collections.Generic;

    public class OctreeObject
    {
        public GameObject Object;
        public Vector3 Position;

        public OctreeObject(GameObject obj)
        {
            Object = obj;
            Position = obj.transform.position;
            obj.AddComponent<OctreeReference>().OctreeObject = this;
        }
    }

    public class OctreeReference : MonoBehaviour
    {
        public OctreeObject OctreeObject;
    }

    public class OctreeNode
    {
        private const int MAX_OBJECTS = 4;
        private const int MAX_LEVELS = 5;

        private int _level;
        private List<OctreeObject> _objects;
        private Bounds _bounds;
        private OctreeNode[] _nodes;

        public OctreeNode(int level, Bounds bounds)
        {
            this._level = level;
            this._bounds = bounds;
            this._objects = new List<OctreeObject>();
            this._nodes = new OctreeNode[8];
        }

        public void Clear()
        {
            for (int i = 0; i < _objects.Count; i++)
                GameObject.Destroy(_objects[i].Object.GetComponent<OctreeReference>());

            _objects.Clear();
            for (int i = 0; i < _nodes.Length; i++)
            {
                if (_nodes[i] != null)
                {
                    _nodes[i].Clear();
                    _nodes[i] = null;
                }
            }
        }

        public bool Remove(OctreeObject octreeObject)
        {
            if (_nodes[0] != null)
            {
                int index = GetIndex(new Bounds(octreeObject.Position, Vector3.zero));
                if (index != -1)                
                    return _nodes[index].Remove(octreeObject);           
            }

            return _objects.Remove(octreeObject);
        }

        private void Split()
        {
            float subWidth = _bounds.size.x / 2f;
            float subHeight = _bounds.size.y / 2f;
            float subDepth = _bounds.size.z / 2f;
            Vector3 size = new Vector3(subWidth, subHeight, subDepth);
            Vector3 center = _bounds.center;

            _nodes[0] = new OctreeNode(_level + 1, new Bounds(center + new Vector3(subWidth / 2, subHeight / 2, subDepth / 2), size));
            _nodes[1] = new OctreeNode(_level + 1, new Bounds(center + new Vector3(-subWidth / 2, subHeight / 2, subDepth / 2), size));
            _nodes[2] = new OctreeNode(_level + 1, new Bounds(center + new Vector3(-subWidth / 2, -subHeight / 2, subDepth / 2), size));
            _nodes[3] = new OctreeNode(_level + 1, new Bounds(center + new Vector3(subWidth / 2, -subHeight / 2, subDepth / 2), size));
            _nodes[4] = new OctreeNode(_level + 1, new Bounds(center + new Vector3(subWidth / 2, subHeight / 2, -subDepth / 2), size));
            _nodes[5] = new OctreeNode(_level + 1, new Bounds(center + new Vector3(-subWidth / 2, subHeight / 2, -subDepth / 2), size));
            _nodes[6] = new OctreeNode(_level + 1, new Bounds(center + new Vector3(-subWidth / 2, -subHeight / 2, -subDepth / 2), size));
            _nodes[7] = new OctreeNode(_level + 1, new Bounds(center + new Vector3(subWidth / 2, -subHeight / 2, -subDepth / 2), size));
        }

        private int GetIndex(Bounds bounds)
        {
            int index = -1;
            Vector3 midpoint = this._bounds.center;

            bool top = bounds.min.y > midpoint.y;
            bool bottom = bounds.max.y < midpoint.y;

            bool left = bounds.max.x < midpoint.x;
            bool right = bounds.min.x > midpoint.x;

            bool front = bounds.min.z > midpoint.z;
            bool back = bounds.max.z < midpoint.z;

            if (front)
            {
                if (top)
                {
                    if (right)
                    {
                        index = 0;
                    }
                    else if (left)
                    {
                        index = 1;
                    }
                }
                else if (bottom)
                {
                    if (right)
                    {
                        index = 3;
                    }
                    else if (left)
                    {
                        index = 2;
                    }
                }
            }
            else if (back)
            {
                if (top)
                {
                    if (right)
                    {
                        index = 4;
                    }
                    else if (left)
                    {
                        index = 5;
                    }
                }
                else if (bottom)
                {
                    if (right)
                    {
                        index = 7;
                    }
                    else if (left)
                    {
                        index = 6;
                    }
                }
            }

            return index;
        }

        public void Insert(OctreeObject octreeObject)
        {
            if (_nodes[0] != null)
            {
                int index = GetIndex(new Bounds(octreeObject.Position, Vector3.zero));

                if (index != -1)
                {
                    _nodes[index].Insert(octreeObject);
                    return;
                }
            }

            _objects.Add(octreeObject);

            if (_objects.Count > MAX_OBJECTS && _level < MAX_LEVELS)
            {
                if (_nodes[0] == null)
                {
                    Split();
                }

                int i = 0;
                while (i < _objects.Count)
                {
                    int index = GetIndex(new Bounds(_objects[i].Position, Vector3.zero));
                    if (index != -1)
                    {
                        _nodes[index].Insert(_objects[i]);
                        _objects.RemoveAt(i);
                    }
                    else
                    {
                        i++;
                    }
                }
            }
        }

        public void Retrieve(List<OctreeObject> returnObjects, Bounds bounds)
        {
            int index = GetIndex(bounds);
            if (index != -1 && _nodes[0] != null)
                _nodes[index].Retrieve(returnObjects, bounds);

            returnObjects.AddRange(_objects);
        }
    }


    public class Octree
    {
        private OctreeNode _rootNode;

        public Octree(Bounds bounds) =>
            _rootNode = new OctreeNode(0, bounds);       

        public void Clear() =>
            _rootNode.Clear();        

        public void Insert(GameObject obj)
        {
            OctreeObject octreeObject = new OctreeObject(obj);
            _rootNode.Insert(octreeObject);
        }

        public List<OctreeObject> Retrieve(Bounds bounds)
        {
            List<OctreeObject> returnObjects = new List<OctreeObject>();
            _rootNode.Retrieve(returnObjects, bounds);
            return returnObjects;
        }

        public bool Remove(GameObject obj)
        {
            OctreeReference reference = obj.GetComponent<OctreeReference>();
            if (reference != null && reference.OctreeObject != null)
                return _rootNode.Remove(reference.OctreeObject);

            return false;
        }
    }

}
