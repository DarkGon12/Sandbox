using QuadTree;
using Unity.VisualScripting.Antlr3.Runtime.Tree;
using UnityEngine;

namespace OctoTree
{
    public class TreeFinder : MonoBehaviour
    {
        [SerializeField] private Bounds _worldBounds;
        [SerializeField] private bool _canDrawGizma;

        private Octree _octree;

        private void Start()
        {
            GenerateTree();
        }

        private void GenerateTree()
        {
            _octree = new Octree(_worldBounds);

            GameObject[] trees = GameObject.FindGameObjectsWithTag("Asteroid");
            foreach (var tree in trees)
            {
                _octree.Insert(tree);
            }
        }

        [ContextMenu("Debug regenerate")]
        public void RegenerateTree()
        {
            _octree.Clear();
            GenerateTree();
        }

        public void InsetrFirstTree(GameObject tree)
        {
            _octree.Insert(tree);
        }

        public GameObject FindClosestTree(Vector3 position)
        {
            GameObject closestTree = null;
            float closestDistanceSqr = Mathf.Infinity;
            Bounds searchBounds = new Bounds(position, new Vector3(1, 1, 1));

            var nearbyTrees = _octree.Retrieve(searchBounds);

            foreach (var treeObj in nearbyTrees)
            {
                float distanceSqr = (treeObj.Position - position).sqrMagnitude;
                if (distanceSqr < closestDistanceSqr)
                {
                    closestDistanceSqr = distanceSqr;
                    closestTree = treeObj.Object;
                }
            }

            return closestTree;
        }

        public void RemoveTree(GameObject obj)
        {
            if (_octree.Remove(obj))
            {
                Destroy(obj.GetComponent<OctreeReference>());
                Debug.Log("Object removed: " + obj.name);
            }
            else
                Debug.Log("Object not found in Octree: " + obj.name);
        }

        private void OnDrawGizmos()
        {
            if(_canDrawGizma)
                Gizmos.DrawCube(_worldBounds.center, _worldBounds.size);
        }
    }
}