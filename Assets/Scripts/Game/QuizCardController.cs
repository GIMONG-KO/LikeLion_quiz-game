using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public struct QuizData
{
    public int index;
    public string question;
    public string description;
    public int type;
    public int answer;
    public string firstOption;
    public string secondOption;
    public string thirdOption;
}

// 퀴즈 카드의 위치 상태를 정의할 클래스가 반드시 구현할(약속) 메서드의 목록
public interface IQuizCardPositionState
{
    void Trasition(bool withAnimation, Action onComplete = null);
}

// 퀴즈 카드의 위치 상태 전이를 관리할 목적
public class QuizCardPositionStateContext
{
    private IQuizCardPositionState _currentState;

    public void SetState(IQuizCardPositionState state, bool withAnimation, Action onComplete = null)
    {
        if (_currentState == state) return;
        
        _currentState = state;
        _currentState.Trasition(withAnimation, onComplete);
    }
}

public class QuizCardPositionState
{
    protected QuizCardController _quizCardController;
    protected RectTransform _rectTransform;
    protected CanvasGroup _canvasGroup;

    public QuizCardPositionState(QuizCardController quizCardController)
    {
        _quizCardController = quizCardController;
        _rectTransform = _quizCardController.gameObject.GetComponent<RectTransform>();
        _canvasGroup = _quizCardController.gameObject.GetComponent<CanvasGroup>();
    }
}

// 퀴즈 카드가 첫 번째 위치에 나타날 상태를 처리할 상태 클래스
public class QuizCardPositionStateFirst: QuizCardPositionState, IQuizCardPositionState
{
    public QuizCardPositionStateFirst(QuizCardController quizCardController) : base(quizCardController) { }

    public void Trasition(bool withAnimation, Action onComplete = null)
    {
        var animationDuration = (withAnimation) ? 0.2f : 0f;
        
        _rectTransform.DOAnchorPos(Vector2.zero, animationDuration);
        _rectTransform.DOScale(1f, animationDuration);
        _canvasGroup.DOFade(1f, animationDuration).OnComplete(() => onComplete?.Invoke());

        _rectTransform.SetAsLastSibling();
    }
}
// 퀴즈 카드가 두 번째 위치에 나타날 상태를 처리할 상태 클래스
public class QuizCardPositionStateSecond: QuizCardPositionState, IQuizCardPositionState
{
    public QuizCardPositionStateSecond(QuizCardController quizCardController) : base(quizCardController) { }

    public void Trasition(bool withAnimation, Action onComplete = null)
    {
        var animationDuration = (withAnimation) ? 0.2f : 0f;
        
        _rectTransform.DOAnchorPos(new Vector2(0f, 160f), 0);
        _rectTransform.DOScale(0.9f, animationDuration);
        _canvasGroup.DOFade(0.7f, animationDuration).OnComplete(() => onComplete?.Invoke());

        _rectTransform.SetAsFirstSibling();
    }
}
// 퀴즈 카드가 사라질 상태를 처리할 상태 클래스
public class QuizCardPositionStateRemove: QuizCardPositionState, IQuizCardPositionState
{
    public QuizCardPositionStateRemove(QuizCardController quizCardController) : base(quizCardController) { }

    public void Trasition(bool withAnimation, Action onComplete = null)
    {
        var animationDuration = (withAnimation) ? 0.2f : 0f;
        _rectTransform.DOAnchorPos(new Vector2(0f, -280f), animationDuration);
        _canvasGroup.DOFade(0f, animationDuration).OnComplete(() => onComplete?.Invoke());
    }
}

// 퀴즈 카드가 뒤집어지는 상태 클래스
public class QuizCardPositionStateFlip : QuizCardPositionState, IQuizCardPositionState
{
    public QuizCardPositionStateFlip(QuizCardController quizCardController) : base(quizCardController) { }
    public void Trasition(bool withAnimation, Action onComplete = null)
    {
        var animationDuration = (withAnimation) ? 0.3f : 0f;

        _rectTransform.DORotate(new Vector3(0, 90, 0), animationDuration / 2)
            .OnComplete(() =>
            {
                _rectTransform.DORotate(new Vector3(0, 0, 0), animationDuration / 2)
                    .OnComplete(() => onComplete?.Invoke());
            });
    }
}

public class QuizCardPositionStateFlipNormal : QuizCardPositionState, IQuizCardPositionState
{
    public QuizCardPositionStateFlipNormal(QuizCardController quizCardController) : base(quizCardController) { }
    public void Trasition(bool withAnimation, Action onComplete = null)
    {
        var animationDuration = (withAnimation) ? 0.3f : 0f;

        _rectTransform.DORotate(new Vector3(0, 90, 0), animationDuration / 2)
            .OnComplete(() =>
            {
                _rectTransform.DORotate(new Vector3(0, 0, 0), animationDuration / 2)
                    .OnComplete(() => onComplete?.Invoke());
            });
    }
}

public class QuizCardController : MonoBehaviour
{
    [SerializeField] private GameObject frontPanel;
    [SerializeField] private GameObject correctBackPanel;
    [SerializeField] private GameObject incorrectBackPanel;
    
    // Front Panel
    [SerializeField] private TMP_Text questionText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Button[] optionButtons;
    [SerializeField] private GameObject threeOptionButtons;
    [SerializeField] private GameObject oxButtons;
    
    // Incorrect Back Panel
    [SerializeField] private TMP_Text heartCountText;
    
    // Timer
    [SerializeField] private GoyaTimer timer;
    
    // 애니메이션
    [SerializeField] private GameObject quizCardResultPanel;
    
    public enum QuizCardPanelType { Front, CorrectBackPanel, InCorrectBackPanel }
    private enum QuizCardResultType { None, Correct, Incorrect }
    public delegate void QuizCardDelegate(int cardIndex);
    private event QuizCardDelegate onCompleted;
    private int _answer;
    private int _quizCardIndex;
    
    private Vector2 _correctBackPanelPosition;
    private Vector2 _incorrectBackPanelPosition;
    
    // 퀴즈 카드 위치 상태
    private IQuizCardPositionState _positionStateFirst;
    private IQuizCardPositionState _positionStateSecond;
    private IQuizCardPositionState _positionStateRemove;
    private IQuizCardPositionState _positionStateFlip;
    private IQuizCardPositionState _positionStateFlipNormal;        // 추가
    private QuizCardPositionStateContext _positionStateContext;
    
    // 애니메이션
    private Animator _animator;
    
    private void Awake()
    {
        // 숨겨진 패널의 좌표 저장
        _correctBackPanelPosition = correctBackPanel.GetComponent<RectTransform>().anchoredPosition;
        _incorrectBackPanelPosition = incorrectBackPanel.GetComponent<RectTransform>().anchoredPosition;
        
        // 상태 관리를 위한 Context 객체 생성
        _positionStateContext = new QuizCardPositionStateContext();
        _positionStateFirst = new QuizCardPositionStateFirst(this);
        _positionStateSecond = new QuizCardPositionStateSecond(this);
        _positionStateRemove = new QuizCardPositionStateRemove(this);
        _positionStateFlip = new QuizCardPositionStateFlip(this);
        _positionStateFlipNormal = new QuizCardPositionStateFlipNormal(this);       // 추가
        _positionStateContext.SetState(_positionStateRemove, false);
        
        _animator = GetComponent<Animator>();
    }

    private void Start()
    {
        timer.OnTimeout = () =>
        {
            // TODO: 오답 연출
            ShowQuizCardResult(QuizCardResultType.Incorrect);
            // SetQuizCardPanelActive(QuizCardPanelType.InCorrectBackPanel);
        };
    }
    
    public enum QuizCardPositionType { First, Second, Remove }
    
    /// <summary>
    /// 퀴즈 카드 위치를 지정하는 메서드
    /// </summary>
    /// <param name="quizCardPositionType">퀴즈 카드 위치</param>
    /// <param name="withAnimation">애니메이션 여부</param>
    /// <param name="onComplete">위치 지정 후 실행할 동작</param>
    public void SetQuizCardPosition(QuizCardPositionType quizCardPositionType,
        bool withAnimation, Action onComplete = null)
    {
        switch (quizCardPositionType)
        {
            case QuizCardPositionType.First:
                _positionStateContext.SetState(_positionStateFirst, withAnimation, () =>
                {
                    timer.InitTimer();
                    timer.StartTimer();
                    onComplete?.Invoke();
                });
                break;
            case QuizCardPositionType.Second:
                _positionStateContext.SetState(_positionStateSecond, withAnimation, () =>
                {
                    timer.InitTimer();
                    onComplete?.Invoke();
                });
                break;
            case QuizCardPositionType.Remove:
                _positionStateContext.SetState(_positionStateRemove, withAnimation, onComplete);
                break;
        }
    }

    public void SetQuiz(QuizData quizData, QuizCardDelegate onCompleted)
    {
        // 1. 퀴즈
        // 2. 설명
        // 3. 타입 (0: OX퀴즈, 1: 보기 3개 객관식)
        // 4. 정답
        // 5. 보기 (1,2,3)
        
        // 퀴즈 카드 인덱스 할당
        _quizCardIndex = quizData.index;
        
        // Front Panel 표시
        SetQuizCardPanelActive(QuizCardPanelType.Front, false);
        
        // 퀴즈 데이터 표현
        questionText.text = quizData.question;
        _answer = quizData.answer;
        descriptionText.text = quizData.description;

        if (quizData.type == 0)
        {
            // 3지선다 퀴즈
            threeOptionButtons.SetActive(true);
            oxButtons.SetActive(false);
            
            var firstButtonText = optionButtons[0].GetComponentInChildren<TMP_Text>();
            firstButtonText.text = quizData.firstOption;
            var secondButtonText = optionButtons[1].GetComponentInChildren<TMP_Text>();
            secondButtonText.text = quizData.secondOption;
            var thirdButtonText = optionButtons[2].GetComponentInChildren<TMP_Text>();
            thirdButtonText.text = quizData.thirdOption;
        }
        else if (quizData.type == 1)
        {
            // OX 퀴즈
            threeOptionButtons.SetActive(false);
            oxButtons.SetActive(true);
        }
        
        this.onCompleted = onCompleted;
        
        // Incorrect Back Panel
        heartCountText.text = GameManager.Instance.heartCount.ToString();
        
        // Quiz Card Result Panel
        ShowQuizCardResult(QuizCardResultType.None);
    }

    /// <summary>
    /// 퀴즈의 정답을 선택하기 위한 버튼
    /// </summary>
    /// <param name="buttonIndex"></param>
    public void OnClickOptionButton(int buttonIndex)
    {
        // Timer 일시 정시
        timer.PauseTimer();
        
        if (buttonIndex == _answer)
        {
            Debug.Log("정답!");
            // TODO: 정답 연출
            // SetQuizCardPanelActive(QuizCardPanelType.CorrectBackPanel);
            ShowQuizCardResult(QuizCardResultType.Correct);
        }
        else
        {
            Debug.Log("오답~");
            // TODO: 오답 연출
            // SetQuizCardPanelActive(QuizCardPanelType.InCorrectBackPanel);
            ShowQuizCardResult(QuizCardResultType.Incorrect);
        }
    }

    public void SetQuizCardPanelActive(QuizCardPanelType quizCardPanelType)
    {
        ShowQuizCardResult(QuizCardResultType.None);
        SetQuizCardPanelActive(quizCardPanelType, true);
    }

    private void ShowQuizCardResult(QuizCardResultType quizCardResultType)
    {
        switch (quizCardResultType)
        {
            case QuizCardResultType.Correct:
                quizCardResultPanel.SetActive(true);
                _animator.SetTrigger("correct");
                break;
            case QuizCardResultType.Incorrect:
                quizCardResultPanel.SetActive(true);
                _animator.SetTrigger("incorrect");
                break;
            case QuizCardResultType.None:
                quizCardResultPanel.SetActive(false);
                break;
        }
    }
    
    public void SetQuizCardPanelActive(QuizCardPanelType quizCardPanelType, bool withAnimation)
    {
        switch (quizCardPanelType)
        {
            case QuizCardPanelType.Front:
                correctBackPanel.SetActive(false);
                incorrectBackPanel.SetActive(false);

                _positionStateContext.SetState(_positionStateFlipNormal, withAnimation, () =>
                {
                    frontPanel.SetActive(true);
                    correctBackPanel.GetComponent<RectTransform>().anchoredPosition = _correctBackPanelPosition;
                    incorrectBackPanel.GetComponent<RectTransform>().anchoredPosition = _incorrectBackPanelPosition;
                });
                break;
            case QuizCardPanelType.CorrectBackPanel:
                frontPanel.SetActive(false);
                incorrectBackPanel.SetActive(false);
                
                _positionStateContext.SetState(_positionStateFlip, withAnimation, () =>
                {
                    correctBackPanel.SetActive(true);
                    correctBackPanel.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
                    incorrectBackPanel.GetComponent<RectTransform>().anchoredPosition = _incorrectBackPanelPosition;
                });
                break;
            case QuizCardPanelType.InCorrectBackPanel:
                frontPanel.SetActive(false);
                correctBackPanel.SetActive(false);

                _positionStateContext.SetState(_positionStateFlip, withAnimation, () =>
                {
                    incorrectBackPanel.SetActive(true);
                    correctBackPanel.GetComponent<RectTransform>().anchoredPosition = _correctBackPanelPosition;
                    incorrectBackPanel.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
                });
                break;
        }    
    }
    
    public void OnClickExitButton()
    {
        
    }

    #region Correct Back Panel
    /// <summary>
    /// 다음 버튼 이벤트
    /// </summary>
    public void OnClickNextQuizButton()
    {
        onCompleted?.Invoke(_quizCardIndex);
    }
    
    #endregion

    #region Incorrect Back Panel

    /// <summary>
    /// 다시도전 버튼 이벤트
    /// </summary>
    public void OnClickRetryQuizButton()
    {
        if (GameManager.Instance.heartCount > 0)
        {
            GameManager.Instance.heartCount--;
            heartCountText.text = GameManager.Instance.heartCount.ToString();
            SetQuizCardPanelActive(QuizCardPanelType.Front, true);
            
            // 타이머 초기화 및 시작
            timer.InitTimer();
            timer.StartTimer();
        }
        else
        {
            // 하트가 부족해서 다시도전 불가
            // TODO: 하트 부족 알림
        }
    }
    
    #endregion
}
