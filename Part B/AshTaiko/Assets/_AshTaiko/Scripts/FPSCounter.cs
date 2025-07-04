using UnityEngine;
using TMPro;

public class FPSCounter : MonoBehaviour
{
    public bool showMs = true;
    public float timer, refresh, avgFramerate;
    private TextMeshProUGUI displayText;
    
    float deltaTime;

    private void Awake()
    {
        displayText = GetComponent<TextMeshProUGUI>();
    }


    private void Update()
    {
        this.deltaTime += (Time.unscaledDeltaTime - this.deltaTime) * 0.1f;
		float ms = this.deltaTime * 1000f;
		float fps = 1f / this.deltaTime;

        float timelapse = Time.unscaledDeltaTime;
        timer = timer <= 0 ? refresh : timer -= timelapse;

        if (timer <= 0) avgFramerate = (int)(1f / timelapse);
        
        if(showMs) 
        {
            displayText.text = string.Format("{0:0.0} ms ({1:0.} fps)", ms, fps);
            return;
        }
    }
}

