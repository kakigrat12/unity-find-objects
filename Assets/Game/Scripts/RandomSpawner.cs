using System.Collections.Generic;
using System.Linq;
using CirclePacking.Fast;
using UnityEngine;

public class RandomSpawner : MonoBehaviour
{
    [SerializeField] private int _count = 50;
    [SerializeField] private List<CircleCollider2D> _prefabs;

    private IEnumerable<CircleCollider2D> Radiuces()
    {
        for(int i = 0; i < _count; i++)
        {
            yield return _prefabs[Random.Range(0, _prefabs.Count)];
        }
    }


    private void OnEnable()
    {
        Spawn();
    }

    public void Spawn()
    {
        foreach (Transform child in transform)
            Destroy(child.gameObject);


        var objs = Radiuces().ToList();
        var packer = new UltraCirclePacker(
            center: System.Numerics.Vector2.Zero,
            maxRadius: 60f
        );


        var points = packer.PackCircles(objs.Select(x => x.radius * x.transform.localScale.x).ToArray());


        for (int i = 0; i < points.Length; i++)
        {
            Instantiate(objs[i], new Vector3(points[i].X, points[i].Y, 0), objs[i].transform.rotation, transform);
        }
    }
}
