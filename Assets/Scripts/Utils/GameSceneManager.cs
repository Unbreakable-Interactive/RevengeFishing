using UnityEngine;
using UnityEngine.SceneManagement;


public enum GameScene
{
    MainMenu,
    Gameplay,
    Victory,
    Lose
}

public static class GameSceneManager
{
    
    public static void LoadScene(GameScene gameScene)
    {
        SceneManager.LoadScene(GetSceneName(gameScene));
    }

    private static string GetSceneName(GameScene gameScene)
    {
        switch (gameScene)
        {
            default:
            case  GameScene.MainMenu:
                return SceneUtils.MAINMENU_SCENE;
            case  GameScene.Gameplay:
                return SceneUtils.GAMEPLAY_SCENE;
            case GameScene.Victory:
                return SceneUtils.VICTORY_SCENE;
            case GameScene.Lose:
                return SceneUtils.LOSE_SCENE;;
        }
    }
}
