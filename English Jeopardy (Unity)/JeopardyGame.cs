using SynodicArc.English.Constants;
using SynodicArc.English.Helpers;
using SynodicArc.English.PrefabObjects;
using SynodicArc.English.Themes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SynodicArc.English.Games.Jeopardy
{
    /// <summary>
    /// Represents the Jeopardy game
    /// </summary>
    public class JeopardyGame : MonoBehaviour
    {

        private TeamInfoPrefabObject[] teamInfoPrefabObjects;

        private List<CategoryQuestionPrefabObject> categoryQuestionPrefabObjects;

        public Transform CategoryHolders;
        public GameObject CategoryPanelPrefabObject_Prefab;
        public GameObject QuestionPrefabObject_Prefab;

        public GameObject QuestionPanel;
        public Text QuestionText;
        public Image QuestionImage;
        public Text MiddleQuestionText;
        public Text AnswerText;
        public Text PointsAmount;
        public Text PointsSpecialModifier;


        /// <summary>
        /// Starts the game of Jeopardy.
        /// </summary>
        public void InitializeJeopardy(Theme theme, int difficulty)
        {
            teamInfoPrefabObjects = GameObject.FindObjectsOfType<TeamInfoPrefabObject>();

            LoadCategoriesAndQuestions(theme, difficulty);
        }
        
        /// <summary>
        /// Loads the categories and categories for this game.
        /// </summary>
        private void LoadCategoriesAndQuestions(Theme theme, int difficulty)
        {
            JeopardyCategory[] categories = ResourceHelper.RetrieveCategories(GameConstants.JeopardyPath, GameConstants.JeopardyCategoriesPath);

            foreach(JeopardyCategory category in categories)
            {
                if (category.ThemeRelated)
                {
                    if (theme != category.ThemeRequired)
                        continue;
                }

                if (category.DifficultyRelated)
                {
                    if (category.DifficultyRetroactive)
                    {
                        if(difficulty <= category.DifficultySetting)
                        {
                            //ok
                        }
                        else
                        {
                            continue;
                        }
                    }
                    else
                    {
                        if (difficulty != category.DifficultySetting)
                            continue;
                    }
                }

                CategoryPanelPrefabObject newCategoryPanel = Instantiate(CategoryPanelPrefabObject_Prefab).GetComponent<CategoryPanelPrefabObject>();
                newCategoryPanel.SetCategoryText(category.CategoryTitle);
                newCategoryPanel.SetCategorySubText(category.CategorySubTitle);
                newCategoryPanel.transform.SetParent(CategoryHolders);
                newCategoryPanel.transform.localScale = CategoryHolders.localScale;

                string pointValueSymbol = "";
                switch (category.JeopardyCategoryType)
                {
                    case (JeopardyCategoryTypes.Steal):
                        pointValueSymbol = GameConstants.StealSymbol;
                        break;
                }

                JeopardyQuestion[] categoryQuestions = ResourceHelper.RetrieveQuestions(GameConstants.JeopardyPath, GameConstants.JeopardyQuestionsPath, theme.ThemeName + "/", category.CategoryTitle + "/");
                foreach (JeopardyQuestion question in categoryQuestions)
                {
                    if (question.Disabled)
                        continue;

                    if (question.DifficultyRelated)
                    {
                        if (question.DifficultyRetroactive)
                        {
                            if (difficulty <= question.DifficultySetting)
                            {
                                //ok
                            }
                            else
                            {
                                continue;
                            }
                        }
                        else if (question.DifficultyRange)
                        {
                            if (difficulty >= question.DifficultyMin && difficulty <= question.DifficultyMax)
                            {
                                //ok
                            }
                            else
                            {
                                continue;
                            }
                        }
                        else
                        {
                            if (difficulty != question.DifficultySetting)
                                continue;
                        }
                    }

                    CategoryQuestionPrefabObject questionPrefabObj = Instantiate(QuestionPrefabObject_Prefab).GetComponent<CategoryQuestionPrefabObject>();
                    questionPrefabObj.SetQuestion(question, category.JeopardyCategoryType, category.CategoryPointValue);
                    questionPrefabObj.GetComponent<Button>().onClick.AddListener(() => { SetAndShowQuestion(questionPrefabObj.gameObject); });

                    string displayPointValue = "";
                    if (question.OverridePoints)
                        displayPointValue = question.OverridePointValue.ToString();
                    else
                        displayPointValue = category.CategoryPointValue.ToString();

                    questionPrefabObj.SetQuestionPointsText(displayPointValue + pointValueSymbol);

                    questionPrefabObj.transform.SetParent(newCategoryPanel.GetCategoryPanelQuestionScrollRectTransform());
                    questionPrefabObj.transform.localScale = newCategoryPanel.GetCategoryPanelQuestionScrollRectTransform().localScale;
                }
            }

        }
        

        /// <summary>
        /// Sets the question and shows the panel.
        /// </summary>
        public void SetAndShowQuestion(GameObject questionObject)
        {
            CategoryQuestionPrefabObject thisQuestionPrefab = questionObject.GetComponent<CategoryQuestionPrefabObject>();
            JeopardyQuestion thisQuestion = thisQuestionPrefab.GetQuestion();

            QuestionText.text = thisQuestion.QuestionText;
            if (thisQuestion.ImageSprite != null)
            {
                QuestionImage.sprite = thisQuestion.ImageSprite;
                QuestionImage.gameObject.SetActive(true);
            }
            if (!string.IsNullOrEmpty(thisQuestion.MiddleQuestionText))
            {
                MiddleQuestionText.text = thisQuestion.MiddleQuestionText;
                MiddleQuestionText.gameObject.SetActive(true);
            }
            AnswerText.text = thisQuestion.AnswerText;

            PointsAmount.text = thisQuestion.GetQuestionPointValue().ToString();
            if (thisQuestion.GetJeopardyCategoryType() == JeopardyCategoryTypes.Steal)
            {
                PointsSpecialModifier.text = GameConstants.StealSymbol;
            }

            QuestionPanel.SetActive(true);


            //Finalize by destroying the object so we don't pick it again.
            Destroy(questionObject);
        }


        /// <summary>
        /// Displays the answer field.
        /// </summary>
        public void ShowAnswer()
        {
            AnswerText.gameObject.SetActive(true);
        }


        /// <summary>
        /// Closes the question panel and resets all fields.
        /// </summary>
        public void CloseQuestionPanel()
        {
            QuestionPanel.SetActive(false);
            QuestionText.text = "";
            QuestionImage.sprite = null;
            QuestionImage.gameObject.SetActive(false);
            MiddleQuestionText.text = "";
            MiddleQuestionText.gameObject.SetActive(false);
            AnswerText.text = "";
            AnswerText.gameObject.SetActive(false);
            PointsAmount.text = "1";
            PointsSpecialModifier.text = "";
        }

    }
}