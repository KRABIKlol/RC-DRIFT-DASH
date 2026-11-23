using UnityEngine;
using UnityEngine.InputSystem;

namespace RCDriftDash
{
    [DefaultExecutionOrder(-200)]
    public sealed class RaceSession : MonoBehaviour
    {
        public enum Phase { Placement, Ready, Countdown, Driving, Paused, Result }
        public Phase State { get; private set; }
        public HandControls hands;
        public RoomPlacement placement;
        public DriftCar car;
        public Transform spawn, panel;
        public Camera desktopCamera;
        public GameObject xrRoot, roomRoot;
        public TextMesh title, info;
        public TextMesh[] buttons;
        public BoxCollider[] targets;
        public LineRenderer beam;
        public AudioSource speaker;
        public AudioClip tick, go, finish, record;
        [Min(1)] public int laps = 1;
        public int gateCount = 8;
        private int expected, lap, lastBeat;
        private float clock, remaining, best, nextDisplay, tipUntil;
        private bool improved, restartCountdown, rePlace;
        private string tip;
        private Transform viewer;
        private string Key => (hands.Desktop ? "RCDriftDash.Practice.Harbor.v1." : "RCDriftDash.Harbor.v1.") + laps;
        public int NextGate => expected;
        public float Elapsed => clock;

        private void Awake()
        {
            hands.Desktop = Application.isEditor && !UnityEngine.XR.XRSettings.isDeviceActive;
            xrRoot.SetActive(!hands.Desktop);
            roomRoot.SetActive(!hands.Desktop);
            desktopCamera.gameObject.SetActive(hands.Desktop);
        }

        private void Start()
        {
            viewer = hands.Desktop ? desktopCamera.transform : hands.Head;
            best = PlayerPrefs.GetFloat(Key,0);
            if (!float.IsFinite(best) || best < 0) best = 0;
            NewPlacement();
        }

        private void NewPlacement()
        {
            State = Phase.Placement;
            car.SetMotion(false);
            car.gameObject.SetActive(false);
            placement.track.gameObject.SetActive(false);
            hands.gameObject.SetActive(false);
            rePlace = false;
            clock = 0;
            nextDisplay = 0;
        }

        private void Ready()
        {
            car.Park(spawn);
            expected = 1;
            lap = 1;
            clock = 0;
            tipUntil = 0;
            State = Phase.Ready;
            nextDisplay = 0;
        }

        private void Pause()
        {
            if (State != Phase.Driving && State != Phase.Countdown) return;
            restartCountdown = State == Phase.Countdown;
            car.SetMotion(false);
            State = Phase.Paused;
            nextDisplay = 0;
        }

        private void OnApplicationPause(bool paused) { if (paused) Pause(); }
        private void OnApplicationFocus(bool focused) { if (!focused) Pause(); }

        private void Update()
        {
            var ray = hands.Desktop ? new Ray(viewer.position,viewer.forward) : hands.Pointer;
            if (hands.Desktop && Mouse.current != null)
                ray = desktopCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (State == Phase.Placement)
            {
                if (!placement.Valid || PointedButton(ray, out _) < 0) placement.Aim(ray, viewer.eulerAngles.y, hands.Desktop);
                var yaw = Quaternion.Euler(0,viewer.eulerAngles.y,0);
                panel.SetPositionAndRotation(viewer.position + yaw * new Vector3(0,.05f,.85f),yaw);
                if (hands.Desktop) panel.SetPositionAndRotation(new Vector3(0,.25f,.46f),Quaternion.identity);
            }
            else
            {
                hands.Sample();
                if (!hands.Desktop && !placement.Stable)
                {
                    if (State == Phase.Ready || State == Phase.Result) { NewPlacement(); return; }
                    rePlace = true;
                    Pause();
                }
                if (!hands.Tracked || !hands.Held) Pause();
            }

            if (State == Phase.Countdown)
            {
                remaining -= Time.deltaTime;
                int beat = Mathf.CeilToInt(remaining);
                if (remaining <= 0)
                {
                    State = Phase.Driving;
                    car.SetMotion(true);
                    speaker.PlayOneShot(go);
                }
                else if (beat != lastBeat) { speaker.PlayOneShot(tick); lastBeat = beat; }
            }
            if (State == Phase.Driving)
            {
                clock += Time.deltaTime;
                car.throttle = hands.Gas;
                car.steering = hands.Steer;
                var local = placement.track.InverseTransformPoint(car.body.position);
                if (Mathf.Max(Mathf.Abs(local.x), Mathf.Abs(local.z)) > .56f || Mathf.Abs(local.y) > .16f) Ready();
            }

            if (Time.unscaledTime >= nextDisplay) { Draw(); nextDisplay = Time.unscaledTime + .06f; }
            int pointed = PointedButton(ray,out float length);
            beam.enabled = !hands.Desktop;
            if (beam.enabled)
            {
                if (State == Phase.Placement && placement.preview.gameObject.activeSelf && pointed < 0)
                    length = Vector3.Distance(ray.origin,placement.preview.position);
                beam.SetPosition(0,ray.origin); beam.SetPosition(1,ray.GetPoint(length));
            }
            for (int i=0;i<buttons.Length;i++) buttons[i].color = i==pointed ? Color.yellow : Color.white;
            bool click = hands.Desktop && Mouse.current?.leftButton.wasPressedThisFrame == true;
            if (hands.Select || (click && pointed>=0))
                Act(pointed >= 0 ? pointed : 0);
            else if (hands.Alternate) Act(State == Phase.Driving || State==Phase.Countdown ? 0 : 1);
            if (hands.Desktop && Keyboard.current?.rKey.wasPressedThisFrame == true && State != Phase.Placement) Ready();
        }

        private int PointedButton(Ray ray,out float length)
        {
            length=1.5f;
            for(int i=0;i<targets.Length;i++)
                if(targets[i].gameObject.activeInHierarchy && targets[i].Raycast(ray,out var hit,4))
                { length=hit.distance; return i; }
            return -1;
        }

        public void Act(int button)
        {
            nextDisplay = 0;
            if (State == Phase.Placement)
            {
                if (button == 1 && !hands.Desktop) { placement.Reload(); return; }
                if (button != 0 || !placement.Valid || !hands.Tracked) return;
                placement.Confirm(hands.Desktop);
                car.gameObject.SetActive(true);
                hands.gameObject.SetActive(true);
                hands.Dock(viewer);
                panel.SetPositionAndRotation(placement.track.TransformPoint(0,.21f,.48f),placement.track.rotation);
                Ready();
                return;
            }
            if (button == 2 || rePlace) { NewPlacement(); return; }
            if (State == Phase.Result || (button == 1 && State == Phase.Paused)) { Ready(); return; }
            if (State == Phase.Driving || State == Phase.Countdown) { Pause(); return; }
            if (State == Phase.Ready && button == 1) { NewPlacement(); return; }
            if (!hands.Held || !hands.Tracked || hands.Gas > .08f || Mathf.Abs(hands.Steer) > .12f) return;
            if (State == Phase.Paused && !restartCountdown)
            { State = Phase.Driving; car.SetMotion(true); }
            else { State = Phase.Countdown; remaining = 3; lastBeat = 0; }
        }

        public void Cross(int index, float forwardSpeed)
        {
            if (State != Phase.Driving || forwardSpeed <= .015f) return;
            if (expected != index)
            {
                if (index == 0) { tip = "Пройдите пропущенные ворота"; tipUntil = Time.time + 2; }
                return;
            }
            if (index != 0) { expected = (index+1) % gateCount; return; }
            if (lap < laps) { lap++; expected = 1; return; }
            State = Phase.Result;
            car.SetMotion(false);
            if (clock <= 0 || !float.IsFinite(clock)) { Ready(); return; }
            improved = best <= 0 || clock < best;
            speaker.PlayOneShot(finish);
            if (improved)
            {
                best = clock;
                PlayerPrefs.SetFloat(Key,best); PlayerPrefs.Save();
                speaker.PlayOneShot(record,.65f);
            }
            nextDisplay = 0;
        }

        private void Draw()
        {
            string a="", b="", c="";
            string time = clock.ToString("0.00") + " с";
            string pb = best>0 ? best.ToString("0.00")+" с" : "—";
            switch(State)
            {
                case Phase.Placement:
                    title.text = "RC DRIFT DASH";
                    info.text = placement.Loading ? "Загружаем комнату…" : placement.Hint;
                    a="Установить"; b=hands.Desktop ? "" : "Комната"; break;
                case Phase.Ready:
                    title.text="ПОДГОТОВКА";
                    info.text=hands.Held ? "Руль прямо · отпустите газ · A / Enter — старт" :
                        hands.Desktop ? "G — взять пульт и руль" : "Возьмите пульт и руль боковыми кнопками хвата";
                    a="Старт"; b="Переставить"; break;
                case Phase.Countdown:
                    title.text=Mathf.Max(1,Mathf.CeilToInt(remaining)).ToString();
                    info.text="Приготовьтесь"; a="Пауза"; break;
                case Phase.Driving:
                    title.text=time;
                    info.text=Time.time < tipUntil ? tip : $"Круг {lap}/{laps}   ·   Ворота {(expected==0?"Ф":expected.ToString())}   ·   Рекорд {pb}";
                    a="Пауза"; break;
                case Phase.Paused:
                    title.text="ПАУЗА";
                    info.text=rePlace ? "Привязка комнаты изменилась. Установите трассу снова." :
                        "Возьмите оба предмета · руль прямо · отпустите газ";
                    a=rePlace ? "Установить" : "Продолжить"; b="На старт"; c="Переставить"; break;
                case Phase.Result:
                    title.text=improved ? "НОВЫЙ РЕКОРД" : "ФИНИШ";
                    info.text=$"Время {time}   ·   Лучшее {pb}"; a="Повторить"; c="Переставить"; break;
            }
            Button(0,a); Button(1,b); Button(2,c);
        }

        private void Button(int slot,string caption)
        {
            buttons[slot].transform.parent.gameObject.SetActive(caption.Length>0);
            buttons[slot].text=caption;
        }
    }
}



