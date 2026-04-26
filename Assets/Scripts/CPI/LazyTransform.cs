using UnityEngine;

public class LazyTransform : MonoBehaviour {
    public enum UpdateMode {
        Lazy,
        Immediately
    }

    private Vector3 lPosition;
    private Quaternion lRotation;
    private Vector3 lScale;

    public Vector3 LocalPosition {
        get => lPosition;
        set {
            if (updateMode == UpdateMode.Immediately) {
                transform.localPosition = value;
                lPosition = value;
                return;
            }

            positionDirty = true;
            keepWPositionTarget = false;
            lPosition = value;

            wPosition = transform.parent?.TransformPoint (lPosition) ?? lPosition;
        }
    }
    public Quaternion LocalRotation {
        get => lRotation;
        set {
            if (updateMode == UpdateMode.Immediately) {
                transform.localRotation = value;
                lRotation = value;
                return;
            }

            rotationDirty = true;
            keepWRotationTarget = false;
            lRotation = value;

            var lFwd = lRotation * Vector3.forward;
            var lUp = lRotation * Vector3.up;

            var wFwd = transform.parent?.TransformDirection (lFwd) ?? lFwd;
            var wUp = transform.parent?.TransformDirection (lUp) ?? lUp;

            wRotation = Quaternion.LookRotation (wFwd, wUp);
        }
    }
    public Vector3 LocalScale {
        get => lScale;
        set {
            if (updateMode == UpdateMode.Immediately) {
                transform.localScale = value;
                lScale = value;
                return;
            }

            scaleDirty = true;
            keepWScaleTarget = false;
            lScale = value;
        }
    }

    private Vector3 wPosition;
    private Quaternion wRotation;
    private Vector3 wScale;

    public Vector3 Position {
        get => wPosition;
        set {
            if (updateMode == UpdateMode.Immediately) {
                transform.position = value;
                positionDirty = false;
                keepWPositionTarget = false;
                wPosition = value;
                return;
            }

            positionDirty = true;
            keepWPositionTarget = true;
            wPosition = value;

            lPosition = transform.parent?.InverseTransformPoint (wPosition) ?? wPosition;
        }
    }
    public Quaternion Rotation {
        get => wRotation;
        set {
            if (updateMode == UpdateMode.Immediately) {
                transform.rotation = value;
                wRotation = value;
                rotationDirty = false;
                keepWRotationTarget = false;
                return;
            }

            rotationDirty = true;
            keepWRotationTarget = true;
            wRotation = value;

            var wFwd = wRotation * Vector3.forward;
            var wUp = wRotation * Vector3.up;

            var lFwd = transform.parent?.InverseTransformDirection (wFwd) ?? wFwd;
            var lUp = transform.parent?.InverseTransformDirection (wUp) ?? wUp;

            lRotation = Quaternion.LookRotation (lFwd, lUp);
        }
    }
    public Vector3 Scale {
        get => wScale;
        set {
            var localValue = World2LocalScale (value);

            if (updateMode == UpdateMode.Immediately) {
                transform.localScale = localValue;
                wScale = value;
                return;
            }

            scaleDirty = true;
            keepWRotationTarget = true;
            wScale = value;

            lScale = localValue;
        }
    }

    private bool keepWPositionTarget = true;
    private bool keepWRotationTarget = true;
    private bool keepWScaleTarget = true;

    private bool positionDirty = false;
    private bool rotationDirty = false;
    private bool scaleDirty = false;

    public float delayTime = 0.5f;
    public UpdateMode updateMode;

    private void LateUpdate () {
        if (positionDirty) {
            if (keepWPositionTarget) {
                transform.position = Vector3.MoveTowards (transform.position, Position, Mathf.Max (0.001f, Time.deltaTime * (Position - transform.position).magnitude / delayTime));
                if (transform.position == Position)
                    positionDirty = false;
            } else {
                transform.localPosition = Vector3.MoveTowards (transform.localPosition, LocalPosition, Mathf.Max (0.001f, Time.deltaTime * (LocalPosition - transform.localPosition).magnitude / delayTime));
                if (transform.localPosition == LocalPosition)
                    positionDirty = false;
            }
        }
        if (rotationDirty) {
            if (keepWRotationTarget) {
                transform.rotation = Quaternion.RotateTowards (transform.rotation, Rotation, Mathf.Max (0.01f, Time.deltaTime * Quaternion.Angle (transform.rotation, Rotation) / delayTime));
                if (transform.rotation == Rotation)
                    rotationDirty = false;
            } else {
                transform.localRotation = Quaternion.RotateTowards (transform.localRotation, LocalRotation, Mathf.Max (0.01f, Time.deltaTime * Quaternion.Angle (transform.localRotation, LocalRotation) / delayTime));
                if (transform.localRotation == LocalRotation)
                    rotationDirty = false;
            }
        }
        if (scaleDirty) {
            if (keepWScaleTarget) {
                var localScale = World2LocalScale (Scale);
                transform.localScale = Vector3.MoveTowards (transform.localScale, localScale, Mathf.Max (0.001f, Time.deltaTime * (localScale - transform.localScale).magnitude / delayTime));
                if (transform.localScale == localScale)
                    scaleDirty = false;
            } else {
                transform.localScale = Vector3.MoveTowards (transform.localScale, LocalScale, Mathf.Max (0.001f, Time.deltaTime * (LocalScale - transform.localScale).magnitude / delayTime));
                if (transform.localScale == LocalScale)
                    scaleDirty = false;
            }
        }

    }

    private Vector3 World2LocalScale (Vector3 wScale) {
        var fwd = transform.forward * wScale.z;
        var up = transform.up * wScale.y;
        var right = transform.right * wScale.x;

        var scaleZ = transform.parent?.InverseTransformVector (fwd).magnitude ?? wScale.z;
        var scaleY = transform.parent?.InverseTransformVector (up).magnitude ?? wScale.y;
        var scaleX = transform.parent?.InverseTransformVector (right).magnitude ?? wScale.x;

        return new Vector3 (scaleX, scaleY, scaleZ);
    }
    public void Stop () {
        var previousUpdateMode = updateMode;
        updateMode = UpdateMode.Immediately;
        Position = transform.position;
        Rotation = transform.rotation;
        LocalScale = transform.localScale;
        updateMode = previousUpdateMode;
    }
}