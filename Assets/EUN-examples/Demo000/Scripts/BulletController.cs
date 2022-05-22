namespace XmobiTea.EUN.Demo000
{
    using UnityEngine;

    public class BulletController : MonoBehaviour
    {
        [SerializeField]
        private Rigidbody rb;

        public void SetVelocity(Vector3 velocity)
        {
            rb.velocity = velocity;
        }
    }
}
