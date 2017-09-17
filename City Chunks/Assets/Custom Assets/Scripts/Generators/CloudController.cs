using UnityEngine;
using System.Linq;

[RequireComponent(typeof(ParticleSystem))]
public class CloudController : MonoBehaviour {
 public float maxDistance = 3400f;
 public float cloudHeight = 1500f;
 public float updateInterval = 15f;

 float nextUpdate = 0.0f;
 ParticleSystem ps;
 void Start() {
   ps = this.GetComponent<ParticleSystem>();
   nextUpdate = Time.time + updateInterval;
 }
 void Update() {
   if (Time.time < nextUpdate) return;
   nextUpdate = Time.time + updateInterval;
   transform.position =
       new Vector3(transform.position.x, cloudHeight, transform.position.z);
   ParticleSystem.Particle[] particles =
       new ParticleSystem.Particle[ps.particleCount];
   ps.GetParticles(particles);
   var distanceParticles =
       particles
           .Where(p => Vector3.Distance(transform.position, p.position) <
                      maxDistance)
           .ToArray();
   ps.SetParticles(distanceParticles, distanceParticles.Length);
  }
}
