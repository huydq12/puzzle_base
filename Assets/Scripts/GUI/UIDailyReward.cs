using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public class UIDailyReward : UIPopup
{
    [SerializeField] private GameObject BG;
    [SerializeField] private Button btn_clam;
    [SerializeField] List<DataDailyBonus> imagesDataBonus;
    [SerializeField] List<Sprite> sprites;
   

    [Header("Item Collection")]
    [SerializeField] protected ItemDailyBonus m_itemCollection;
    [SerializeField] protected ItemDailyBonus m_itemCollectionSpecial;
    [SerializeField] protected Transform m_parentContainBtn;
    [SerializeField] protected int num = 12;
    [SerializeField] protected int startIndex;

    [Header("VFX Cach")]
    [SerializeField] private GameObject m_CurrencyPrefab;
    [SerializeField] private Transform target;
    [SerializeField] private RectTransform bubblePos;

    [SerializeField] private Text userCoinTxt;

    private readonly List<GameObject> activeFx = new List<GameObject>();

    private void Start()
    {
        if (!GameManagerInGame.Instance.userData.isFirstClaimDailyReward)
        {
            GameManagerInGame.Instance.userData.dailyBonus.currentIndex = 0;
            GameManagerInGame.Instance.userData.isFirstClaimDailyReward = true;
        }
        else if (GameManagerInGame.Instance.userData.isShowDailyReward)
        {
            btn_clam.SetActive(false);
        }

        btn_clam.onClick.AddListener(Clam);
        SetupImagePopup();

        CreateButton();
    }

    private void Clam()
    {
        btn_clam.enabled = false;
        
        GameManagerInGame.Instance.userData.isShowDailyReward = true;
        if (imagesDataBonus[GameManagerInGame.Instance.userData.dailyBonus.currentIndex].Cach != 0)
        {
            NextGame(imagesDataBonus[GameManagerInGame.Instance.userData.dailyBonus.currentIndex].Cach);
        }
        else
        {
            NextGame(0);
        }
        GameManagerInGame.Instance.userData.dailyBonus.currentIndex++;
    }

    private void SetupImagePopup()
    {
        if(GameManagerInGame.Instance.userData.dailyBonus.currentIndex > 6)
        {
            GameManagerInGame.Instance.userData.dailyBonus.currentIndex = 6;
            btn_clam.SetActive(false);
        }
    }

    public virtual void CreateButton()
    {
        for (int i = startIndex; i < num + startIndex; i++)
        {
            if (i < num - 1)
            {
                ItemDailyBonus rowObject = Instantiate(m_itemCollection, m_parentContainBtn);
                rowObject.SetUp(i, sprites[(int)imagesDataBonus[i].enumType], (int)imagesDataBonus[i].Cach, (string)imagesDataBonus[i].SpecialItem);
            }
            else
            {
                m_itemCollectionSpecial.SetUp(i, sprites[(int)imagesDataBonus[i].enumType], (int)imagesDataBonus[i].Cach, (string)imagesDataBonus[i].SpecialItem);
            }

        }
    }
    
    public void UpdateCash(int cash)
    {
        userCoinTxt.text = GameHelper.GetPrettyCurrency(cash).ToString();
    }

    

    public override void Show()
    {
        base.Show();
        UserData userData = Game.Data.Load<UserData>();
        UpdateCash(userData.playerCash);
    }

    private void NextGame(int value)
    {
        UserData userData = Game.Data.Load<UserData>();
        StartCoroutine(this.ShowCoinFxMoveToUserCoinText(userData.playerCash, userData.playerCash + value));
        userData.playerCash += value;
    }

    private IEnumerator ShowCoinFxMoveToUserCoinText(int fromAmount, int toAmount)
    {
        yield return new WaitForSeconds(0.25f);
        this.PlayCoinFx();
        yield return new WaitForSeconds(1.25f);
        var currentValue = (float)fromAmount;
        var targetCoin = (float)toAmount;
        DOTween.To(() => currentValue, x => currentValue = (int)x, targetCoin, 1f)
            .OnUpdate(() => { this.userCoinTxt.text = $"{currentValue}"; });
        yield return new WaitForSeconds(1.5f);
        UpdateCash(GameManagerInGame.Instance.userData.playerCash);

        UIManager.Instance.Get<UIInGame>().Show();
        Hide();
        
    }


    private void PlayCoinFx()
    {
        for (int i = 0; i < 10; i++)
        {
            Flying();
        }
    }

    private void Flying()
    {
        if (m_CurrencyPrefab == null || bubblePos == null || target == null) return;

        GameObject go = Instantiate(m_CurrencyPrefab, bubblePos.transform);
        if (go == null) return;

        activeFx.Add(go);

        go.transform.position = bubblePos.transform.position;
        DOTween.Kill(go.transform, false);

        go.transform.DORotate(new Vector3(0, 0, Random.Range(0, 180)), 0.5f).SetEase(Ease.Linear);
        go.transform.DOLocalMove(new Vector3(Random.Range(-300, 300), Random.Range(-300, 300), 0), 0.5f).OnComplete(() =>
        {
            if (go == null || !go.activeInHierarchy) return;
            go.transform.DORotate(new Vector3(0, 0, 70), 0.3f).SetEase(Ease.Linear);
            go.transform.DOScale(Vector3.zero, 1.7f);
            go.transform.DOMove(target.position, 0.5f)
                .OnComplete(() =>
                {
                    if (go == null) return;
                    DOTween.Kill(go.transform, false);
                    activeFx.Remove(go);
                    Destroy(go);
                });
        });
    }

    public override void Hide()
    {
        CleanupFx();
        StopAllCoroutines();
        base.Hide();
    }

    protected override void OnDestroy()
    {
        CleanupFx();
        base.OnDestroy();
    }

    private void CleanupFx()
    {
        for (int i = activeFx.Count - 1; i >= 0; i--)
        {
            var go = activeFx[i];
            if (go == null) continue;
            DOTween.Kill(go.transform, false);
            Destroy(go);
        }
        activeFx.Clear();
    }
}


public enum EnumTypeBonus
{
    D1,
    D2,
    D3,
    D4,
    D5,
    D6,
    D7
}

[System.Serializable]
public class DataDailyBonus
{
    public EnumTypeBonus enumType;
    public int Cach;
    public int Diamond;
    public string SpecialItem;
}
