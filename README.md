# Sign Language Recognition and Translation using Kinect Sensor

Using Kinect Sensor Version 1 

---
## Steps to follow:

- Install [KINECT SDK 1.8](https://www.microsoft.com/en-in/download/details.aspx?id=40278)

- Install [Kinect Developer Toolkit](https://www.microsoft.com/en-us/download/details.aspx?id=40276)

- Install [Visual Studio 2022](https://visualstudio.microsoft.com/vs/community/)

- In VSCode, creating a virtual environment: 
    `python -m venv myenv`
- If you are in cmd: `myenv\Scripts\activate.bat`
- If you are in PowerShell: `Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass` and `.\myenv\Scripts\Activate.ps1`
- Virtual environment creation and activation done! 
- Download necessary libraries:
```bash
pip install moviepy==1.0.3 
pip install -r requirements.txt
python newapp.py
```
- Set up Visual Studio Community 2022
- Create new project:
  Select WPF application with *C#* *Windows* *Desktop* 
- Paste .xaml and .cs files from the repository.
- Add references for C#, Kinect, JSON, XAML, and start the project.
- You can add more gesture videos in gesture_videos folder and map them accordingly.
[Sign language videos](https://www.signbsl.com/)
---
### Connect Kinect Sensor to your system and that's it!!
