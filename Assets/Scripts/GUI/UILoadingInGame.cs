using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UILoadingInGame : UIElement
{
    public override bool ManualHide => true;
    public override bool DestroyOnHide => false;
    public override bool UseBehindPanel => false;
}
