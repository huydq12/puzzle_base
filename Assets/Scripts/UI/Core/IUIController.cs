using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;

public interface IUIController
{
    UniTask Show(Type elementType, object data, bool forceShowData, bool hideCurrent, Action onStartShowAction = null);
    BaseUIElement GetExistElement(Type elementType);
    UniTask Hide(Type elementType, Action onStartHideAction = null);
    UniTask HideAll();
    void AddElement<Y>(Type elementType, Y element) where Y : BaseUIElement;
    public bool HasAnyOverlayInStack();
    BaseUIElement CurrentElement { get; }

    public List<BaseUIElement> GetAllElements();

    BaseUIElement GetFirstOverlayElement();
}
