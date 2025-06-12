import torch
import torch.nn as nn
import torch.optim as optim
from torch.utils.data import DataLoader
from dataset import SignLanguageDataset  # Import dataset class

# Define ST-GCN Model
class STGCN(nn.Module):
    def __init__(self, input_dim, hidden_dim, num_classes):
        super(STGCN, self).__init__()
        self.conv1 = nn.Conv1d(input_dim, hidden_dim, kernel_size=3, padding=1)
        self.relu = nn.ReLU()
        self.conv2 = nn.Conv1d(hidden_dim, hidden_dim, kernel_size=3, padding=1)
        self.lstm = nn.LSTM(hidden_dim, hidden_dim, batch_first=True)
        self.fc = nn.Linear(hidden_dim, num_classes)

    def forward(self, x):
        x = x.permute(0, 2, 1)  # Convert to (batch, input_dim, seq_len)
        x = self.relu(self.conv1(x))
        x = self.relu(self.conv2(x))
        x = x.permute(0, 2, 1)  # Convert back to (batch, seq_len, hidden_dim)
        _, (hn, _) = self.lstm(x)
        x = self.fc(hn[-1])
        return x

# ✅ FIX: Only train the model if this file is run directly!
if __name__ == "__main__":
    print("Training Started...")

    # Load Dataset & Train
    json_folder = "D:/Fold1/Padded_Sentences"
    label_map = {"turn right": 0, "give me water": 1, "bus": 2,"wait": 3, "nice to meet you": 4, "up": 5,"how are you": 6, "remove": 7, "run": 8,"stand up": 9, "sick": 10, "sit down": 11,"good morning": 12, "turn left": 13}
    dataset = SignLanguageDataset(json_folder, label_map)
    train_loader = DataLoader(dataset, batch_size=8, shuffle=True)

    num_classes = len(label_map)
    trained_model = STGCN(input_dim=30, hidden_dim=128, num_classes=num_classes)

    # Define Loss and Optimizer
    criterion = nn.CrossEntropyLoss()
    optimizer = optim.Adam(trained_model.parameters(), lr=0.001)

    # Train the Model
    epochs = 200
    for epoch in range(epochs):
        total_loss = 0
        for data, labels in train_loader:
            optimizer.zero_grad()
            outputs = trained_model(data)
            loss = criterion(outputs, labels)
            loss.backward()
            optimizer.step()
            total_loss += loss.item()
        
        print(f"Epoch [{epoch+1}/{epochs}], Loss: {total_loss:.4f}")

    # Save the Model
    torch.save(trained_model.state_dict(), "stgcn_model.pth")
    print("Model saved as stgcn_model.pth")
