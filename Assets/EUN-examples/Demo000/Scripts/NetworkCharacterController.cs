namespace XmobiTea.EUN.Demo000
{
    using UnityEngine;
    using XmobiTea.EUN;
    using XmobiTea.EUN.Common;

    public class NetworkCharacterController : EUNBehaviour
    {
        [SerializeField]
        private Transform turretTransf;

        [SerializeField]
        private Transform shootTransf;

        [SerializeField]
        private Rigidbody rb;

        [SerializeField]
        private BulletController bulletController;

        [SerializeField]
        private float moveSpeed = 5;

        private Vector3 syncPosition;

        private float lastSynchronizationTime = 0f;
        private float syncDelay = 0f;
        private float syncTime = 0f;
        private Vector3 syncStartPosition = Vector3.zero;
        private Quaternion syncStartRotation = Quaternion.identity;
        private Quaternion syncStartTurretRotation = Quaternion.identity;

        private float bodyRotation;
        private float turretRotation;

        public override void OnEUNInitialize(object initializeData)
        {
            var initializeDataArray = initializeData as EUNArray;

            var ownerUserId = initializeDataArray.GetString(0);
            Debug.Log("Owner user id: " + ownerUserId);
        }


        public override void OnEUNSynchronization(object synchronizationData)
        {
            base.OnEUNSynchronization(synchronizationData);

            if (synchronizationData is EUNArray synchronizationDataArray)
            {
                syncPosition.x = synchronizationDataArray.GetFloat(0);
                syncPosition.z = synchronizationDataArray.GetFloat(1);
                bodyRotation = synchronizationDataArray.GetFloat(2);
                turretRotation = synchronizationDataArray.GetFloat(3);

                syncTime = 0f;
                syncDelay = Time.time - lastSynchronizationTime;
                lastSynchronizationTime = Time.time;

                syncStartPosition = transform.position;
                syncStartRotation = transform.rotation;
                syncStartTurretRotation = turretTransf.rotation;
            }
        }

        private void Update()
        {
            if (!eunView.IsMine)
            {
                SyncedTransform();

                return;
            }

            Vector2 moveDir;
            Vector2 turnDir;

            if (Input.GetAxisRaw("Horizontal") == 0 && Input.GetAxisRaw("Vertical") == 0)
            {
                moveDir.x = 0;
                moveDir.y = 0;
            }
            else
            {
                moveDir.x = Input.GetAxis("Horizontal");
                moveDir.y = Input.GetAxis("Vertical");
                Move(moveDir);
            }

            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            var plane = new Plane(Vector3.up, Vector3.up);
            var distance = 0f;
            var hitPos = Vector3.zero;

            if (plane.Raycast(ray, out distance))
            {
                hitPos = ray.GetPoint(distance) - transform.position;
            }

            turnDir = new Vector2(hitPos.x, hitPos.z);

            RotateTurret(new Vector2(hitPos.x, hitPos.z));

            if (Input.GetButton("Fire1"))
                Shoot();
        }

        float nextShoot;
        void Shoot()
        {
            if (nextShoot > Time.time) return;
            nextShoot = Time.time + 0.3f;

            if (eunView.IsMine)
                eunView.RPC(XmobiTea.EUN.Constant.EUNTargets.All, EUNRPCCommand.RPCShoot, shootTransf.position.x, shootTransf.position.y, shootTransf.position.z, turretTransf.rotation.eulerAngles.y);
        }

        [EUNRPC]
        void RPCShoot(float xPos, float yPos, float zPos, float yRot)
        {
            var bullet = Instantiate(bulletController, new Vector3(xPos, yPos, zPos), Quaternion.Euler(0, yRot, 0));
            bullet.SetVelocity(bullet.transform.forward * 5);

            Destroy(bullet.gameObject, 5f);

            Debug.Log("eunView with userId " + eunView.Owner.UserId + " create a bullet at " + xPos + " " + zPos + " " + yRot);
        }

        void RotateTurret(Vector2 direction = default(Vector2))
        {
            if (direction == Vector2.zero)
                return;

            turretRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.y)).eulerAngles.y;
            OnTurretRotation();
        }

        void OnTurretRotation()
        {
            turretTransf.rotation = Quaternion.Euler(0, turretRotation, 0);
        }

        void Move(Vector2 direction = default(Vector2))
        {
            if (direction != Vector2.zero)
            {
                transform.rotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.y))
                                     * Quaternion.Euler(0, Camera.main.transform.eulerAngles.y, 0);

                bodyRotation = transform.rotation.eulerAngles.y;
            }

            Vector3 movementDir = transform.forward * moveSpeed * Time.deltaTime;
            rb.MovePosition(rb.position + movementDir);
        }

        private void SyncedTransform()
        {
            if (Vector2.Distance(transform.position, syncPosition) > 2f || syncTime > 1f)
            {
                rb.position = syncPosition;
            }
            else
            {
                syncTime += Time.deltaTime;
                rb.position = Vector3.Lerp(syncStartPosition, syncPosition, syncTime / syncDelay);
            }

            rb.rotation = Quaternion.Lerp(syncStartRotation, Quaternion.Euler(0, bodyRotation, 0), syncTime / syncDelay);

            turretTransf.rotation = Quaternion.Lerp(syncStartTurretRotation, Quaternion.Euler(0, turretRotation, 0), syncTime / syncDelay);
        }

        public override object GetSynchronizationData()
        {
            if (!eunView.IsMine) return null;

            return new object[]
            {
                rb.position.x,
                rb.position.z,
                bodyRotation,
                turretRotation,
            };
        }
    }
}
