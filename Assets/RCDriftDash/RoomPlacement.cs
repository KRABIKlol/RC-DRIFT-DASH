using Meta.XR.MRUtilityKit;
using UnityEngine;

namespace RCDriftDash
{
    public sealed class RoomPlacement : MonoBehaviour
    {
        public MRUK rooms;
        public Transform preview, track;
        public Renderer previewSurface;
        public Material allowed, blocked;
        public string Hint { get; private set; }
        public bool Valid { get; private set; }
        public bool Loading { get; private set; }
        private MRUKAnchor candidate, locked;
        private Pose original;
        private MRUKRoom lockedRoom;

        public async void Reload()
        {
            if (Loading) return;
            Loading = true;
            try { await rooms.LoadSceneFromDevice(); }
            catch (System.Exception e) { Debug.LogWarning(e.Message); }
            finally { Loading = false; }
        }

        public void Aim(Ray ray, float yaw, bool desktop)
        {
            candidate = null;
            Valid = false;
            var room = desktop ? null : rooms.GetCurrentRoom();
            Vector3 point;
            if (desktop) point = Vector3.zero;
            else if (room == null || !room.Raycast(ray, 4, out RaycastHit hit, out candidate))
            {
                preview.gameObject.SetActive(false);
                Hint = room == null ? "Настройте пространство Quest и разрешите доступ к комнате.\nB — повторить загрузку." : "Наведите правый контроллер на стол или пол.";
                return;
            }
            else point = hit.point;

            preview.SetPositionAndRotation(point + Vector3.up * .003f, Quaternion.Euler(0,yaw,0));
            preview.gameObject.SetActive(true);
            Valid = desktop || Fits(room, candidate, point, preview.rotation);
            Hint = Valid ? "A — установить трассу" : "Нужна свободная поверхность 1,04 × 1,04 м.";
            previewSurface.sharedMaterial = Valid ? allowed : blocked;
        }

        private static bool Fits(MRUKRoom room, MRUKAnchor anchor, Vector3 center, Quaternion rotation)
        {
            if ((anchor.Label & (MRUKAnchor.SceneLabels.FLOOR | MRUKAnchor.SceneLabels.TABLE)) == 0) return false;
            // Reject concave cutouts even when all four corners are inside.
            if (anchor.PlaneBoundary2D != null && Vector3.Dot(anchor.transform.forward, Vector3.up) > .97f)
                foreach (var vertex in anchor.PlaneBoundary2D)
                {
                    var local = Quaternion.Inverse(rotation) * (anchor.transform.TransformPoint(vertex) - center);
                    if (Mathf.Abs(local.x) < .52f && Mathf.Abs(local.z) < .52f) return false;
                }
            for (int x=-2; x<=2; x++)
                for (int z=-2; z<=2; z++)
                {
                    var p = center + rotation * new Vector3(x*.26f, 0, z*.26f);
                    if (!room.Raycast(new Ray(p + Vector3.up*.6f, Vector3.down), .64f, out var hit, out var support)
                        || support != anchor || Mathf.Abs(hit.point.y-center.y) > .012f || hit.normal.y < .97f) return false;
                }
            return true;
        }

        public void Confirm(bool desktop)
        {
            track.SetPositionAndRotation(preview.position, preview.rotation);
            locked = candidate;
            if (!desktop)
            {
                lockedRoom = rooms.GetCurrentRoom();
                original = new Pose(locked.transform.position, locked.transform.rotation);
            }
            preview.gameObject.SetActive(false);
            track.gameObject.SetActive(true);
        }

        public bool Stable => locked && rooms.GetCurrentRoom() == lockedRoom &&
            Vector3.Distance(original.position, locked.transform.position) < .03f &&
            Quaternion.Angle(original.rotation, locked.transform.rotation) < 3f;
    }
}
