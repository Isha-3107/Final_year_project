
from flask import Flask, request, jsonify, render_template,send_from_directory
import torch
import numpy as np
from Stgcn import STGCN
from dataset import SignLanguageDataset
import os

from moviepy.editor import VideoFileClip, concatenate_videoclips
import uuid


app = Flask(__name__)
VIDEO_DIR = "static/output"
os.makedirs(VIDEO_DIR, exist_ok=True)

# Load model once at startup
MODEL_PATH = "gestureRecog_model.pth"
LABEL_MAP = {
    "turn right": 0, "give me water": 1, "bus": 2, "wait": 3, "nice to meet you": 4,
    "up": 5, "how are you": 6, "remove": 7, "run": 8, "stand up": 9, "sick": 10,
    "sit down": 11, "good morning": 12, "turn left": 13, "old": 14, "thank you": 15,
    "time": 16, "what is your name": 17, "you": 18
}

def load_model():
    model = STGCN(input_dim=30, hidden_dim=128, num_classes=len(LABEL_MAP))
    model.load_state_dict(torch.load(MODEL_PATH, map_location=torch.device('cpu')))
    model.eval()
    print("✅ Model Loaded Successfully")
    return model

model = load_model()
predicted_gesture = None

@app.route('/receive_json', methods=['POST'])
def receive_json():
    global predicted_gesture
    data = request.json
    if not data:
        return jsonify({"error": "No data received"}), 400

    try:
        dataset = SignLanguageDataset(json_folder=None, label_map=LABEL_MAP)
        skeleton = dataset.process_skeleton(data)
        input_tensor = torch.tensor(skeleton, dtype=torch.float32).unsqueeze(0)

        with torch.no_grad():
            output = model(input_tensor)
            _, predicted = torch.max(output, 1)

        predicted_gesture = list(LABEL_MAP.keys())[list(LABEL_MAP.values()).index(predicted.item())]
        # ✅ Save prediction to a text file for frontend polling
        #new change for only section loading not full page
        with open('latest_prediction.txt', 'w') as f:
          f.write(predicted_gesture)

        return jsonify({"prediction": predicted_gesture})
    except Exception as e:
        print("Error:", e)
        return jsonify({"error": "Error processing input"}), 500

@app.route('/')
def display_result():
    return render_template('index.html', prediction=predicted_gesture)
  
  #new route
@app.route('/get_latest_prediction')
def get_latest_prediction():
    try:
        with open('latest_prediction.txt', 'r') as f:
            return f.read()
    except FileNotFoundError:
        return "Waiting for prediction..."

@app.route("/speech_to_sign", methods=["POST"])
def speech_to_sign():
    data = request.get_json()
    sentence = data.get("sentence", "")
    words = sentence.lower().split()

    video_clips = []
    for word in words:
        video_path = f"gesture_videos/{word}.mp4"
        if os.path.exists(video_path):
            clip = VideoFileClip(video_path).resize(height=480, width=640)
            #clip = VideoFileClip(video_path).resize(height=480, width=640).set_duration(None).set_fps(30)

            video_clips.append(clip)

    if not video_clips:
        return jsonify({"video_url": None})  # No match found

    final_clip = concatenate_videoclips(video_clips, method="compose")
    output_filename = f"{uuid.uuid4()}.mp4"
    output_path = os.path.join(VIDEO_DIR, output_filename)
    final_clip.write_videofile(output_path, codec="libx264", audio=False,fps=30)
    

    final_clip.close()

    return jsonify({"video_url": f"/static/output/{output_filename}"})

@app.route(f"/{VIDEO_DIR}/<path:filename>")
def serve_generated_video(filename):
    return send_from_directory(VIDEO_DIR, filename)
if __name__ == '__main__':
    app.run(debug=True,threaded=True )

