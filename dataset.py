import torch
import json
import os
import numpy as np
from torch.utils.data import Dataset

class SignLanguageDataset(Dataset):
    def __init__(self, json_folder, label_map, seq_len=50, silent=False):  
        self.json_folder = json_folder
        self.label_map = label_map
        self.seq_len = seq_len
        self.data = []
        self.labels = []

        if json_folder:
            for file in os.listdir(json_folder):
                if file.endswith(".json"):
                    file_path = os.path.join(json_folder, file)
                    with open(file_path, 'r') as f:
                        json_data = json.load(f)
                        skeleton = self.process_skeleton(json_data)
                        label = self.extract_label(json_data, file)  # ✅ Pass filename

                        if label is not None:
                            self.data.append(skeleton)
                            self.labels.append(label)

        if not silent:  
            print(f"Dataset Loaded: {len(self.data)} valid samples.")

    def process_skeleton(self, json_data):
        frames = json_data.get('frames', [])
        skeleton_data = []

        for frame in frames:
            coords = frame.get('joints', {})
            joints_list = ['Head', 'ShoulderCenter', 'ShoulderLeft', 'ShoulderRight', 'ElbowLeft', 
                           'ElbowRight', 'WristLeft', 'WristRight', 'HandLeft', 'HandRight']
            frame_data = []
            for joint in joints_list:
                if joint in coords:
                    frame_data.extend([coords[joint]['x'], coords[joint]['y'], coords[joint]['z']])
                else:
                    frame_data.extend([0, 0, 0])  
            skeleton_data.append(frame_data)

        if not skeleton_data:
            skeleton_data.append([0] * (3 * len(joints_list)))  

        while len(skeleton_data) < self.seq_len:
            skeleton_data.append(skeleton_data[-1])  

        return np.array(skeleton_data[:self.seq_len])

    def extract_label(self, json_data, file_name):
        """ Extracts label from JSON and ensures it exists in label_map. """
        if 'label' in json_data:  
            label = json_data['label']
            if label in self.label_map:  
                return self.label_map[label]
            else:
                print(f"⚠ Warning: Label '{label}' in file '{file_name}' not found in label_map. Skipping file.")
                return None  
        else:
            print(f"⚠ Warning: Label missing in file '{file_name}'. Skipping file.")
            return None  

    def __len__(self):
        return len(self.data)

    def __getitem__(self, idx):
        return torch.tensor(self.data[idx], dtype=torch.float32), torch.tensor(self.labels[idx], dtype=torch.long)
