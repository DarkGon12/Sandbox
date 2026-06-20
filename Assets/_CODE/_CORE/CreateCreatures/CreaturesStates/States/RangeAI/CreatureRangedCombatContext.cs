using UnityEngine;

namespace Creatures.Navigation
{
    public class CreatureRangedCombatContext : MonoBehaviour
    {
        public CreatureCover CurrentCover { get; private set; }
        public CreatureCoverSlot CurrentSlot { get; private set; }
        public Vector3 CurrentCoverNavMeshPosition { get; private set; }
        public Vector3 EnemyPositionWhenCoverWasSelected { get; private set; }

        public float NextShotTime { get; set; }
        public float NextCoverCheckTime { get; set; }
        public float NextDestinationRefreshTime { get; set; }
        public Vector3 LastDestination { get; set; }
        public int ShotsRemainingInBurst { get; set; }

        public bool HasCover => CurrentCover != null && CurrentSlot != null;

        public void SetCover(CreatureCover cover, CreatureCoverSlot slot, Vector3 navMeshPosition, Vector3 enemyPosition)
        {
            ClearCover();

            CurrentCover = cover;
            CurrentSlot = slot;
            CurrentCoverNavMeshPosition = navMeshPosition;
            EnemyPositionWhenCoverWasSelected = enemyPosition;

            CurrentSlot?.Reserve(this);
        }

        public void ClearCover()
        {
            if (CurrentSlot != null)
                CurrentSlot.Release(this);

            CurrentCover = null;
            CurrentSlot = null;
            CurrentCoverNavMeshPosition = Vector3.zero;
            EnemyPositionWhenCoverWasSelected = Vector3.zero;
        }

        public void EnsureBurst(CreatureRangedCombatSettings settings)
        {
            if (settings == null)
                return;

            if (ShotsRemainingInBurst <= 0)
                ShotsRemainingInBurst = settings.RollShotsPerAttack();
        }

        public void StartBurst(int shots)
        {
            ShotsRemainingInBurst = Mathf.Max(1, shots);
        }

        public bool CanShootNow => Time.time >= NextShotTime;

        public void RegisterShot(CreatureRangedCombatSettings settings)
        {
            if (settings == null)
                return;

            EnsureBurst(settings);
            ShotsRemainingInBurst--;

            if (ShotsRemainingInBurst <= 0)
            {
                ShotsRemainingInBurst = settings.RollShotsPerAttack();
                NextShotTime = Time.time + settings.BurstCooldown;
                return;
            }

            NextShotTime = Time.time + settings.ShotInterval;
        }

        private void OnDestroy()
        {
            ClearCover();
        }
    }
}
