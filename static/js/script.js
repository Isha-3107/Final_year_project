// Toggle dark mode
document.addEventListener("DOMContentLoaded", () => {
    const toggle = document.getElementById("darkModeToggle");
    toggle.addEventListener("change", () => {
      document.body.classList.toggle("dark-mode", toggle.checked);
    });
  });
  
  function uploadImage() {
    const fileInput = document.getElementById("imageInput");
    const file = fileInput.files[0];
    if (!file) {
      alert("Please select an image.");
      return;
    }
  
    const formData = new FormData();
    formData.append("image", file);
  
    fetch("/predict", {
      method: "POST",
      body: formData,
    })
      .then((response) => response.json())
      .then((data) => {
        document.getElementById("predictionText").innerText = "Prediction: " + data.prediction;
      })
      .catch((error) => {
        console.error("Error:", error);
      });
  }
  
  function speakPrediction() {
    const prediction = document.getElementById("predictionText").innerText;
    if (!prediction) return;
  
    const utterance = new SpeechSynthesisUtterance(prediction);
    speechSynthesis.speak(utterance);
  }
  