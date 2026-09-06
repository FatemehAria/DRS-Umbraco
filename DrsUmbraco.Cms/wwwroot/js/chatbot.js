const chatbotToggle = document.getElementById("chatbot-toggle");

const chatbotPanel = document.getElementById("chatbot-panel");

const chatbotClose = document.getElementById("chatbot-close");

const chatbotInput = document.getElementById("chatbot-input");

const chatbotForm = document.getElementById("chatbot-form");

const chatbotMessages = document.getElementById("chatbot-messages");

const chatbotSend = chatbotForm.querySelector(".chatbot-send");

let isSendingMessage = false;

class ChatbotUnavailableError extends Error {
  constructor(retryAfterSeconds) {
    super("Chatbot is still initializing.");

    this.name = "ChatbotUnavailableError";
    this.retryAfterSeconds = retryAfterSeconds;
  }
}

function openChatbot() {
  chatbotPanel.hidden = false;

  chatbotToggle.setAttribute("aria-expanded", "true");

  scrollToBottom();

  chatbotInput.focus();
}

function closeChatbot() {
  chatbotPanel.hidden = true;

  chatbotToggle.setAttribute("aria-expanded", "false");
}

function appendMessage(text, type) {
  const message = document.createElement("div");

  message.classList.add("chatbot-message", `chatbot-message--${type}`);

  // مهم:
  // از textContent استفاده می‌کنیم تا HTML کاربر
  // داخل صفحه اجرا نشود.
  message.textContent = text;

  chatbotMessages.appendChild(message);

  scrollToBottom();

  return message;
}

function scrollToBottom() {
  chatbotMessages.scrollTop = chatbotMessages.scrollHeight;
}

function showLoading() {
  const loading = document.createElement("div");

  loading.classList.add(
    "chatbot-message",
    "chatbot-message--bot",
    "chatbot-message--loading",
  );

  loading.setAttribute("aria-label", "در حال بررسی");

  for (let i = 0; i < 3; i++) {
    const dot = document.createElement("span");

    dot.classList.add("chatbot-loading-dot");

    loading.appendChild(dot);
  }

  chatbotMessages.appendChild(loading);

  scrollToBottom();

  return loading;
}

function setFormBusy(isBusy) {
  chatbotInput.disabled = isBusy;
  chatbotSend.disabled = isBusy;
}

async function sendMessage(message) {
  const response = await fetch("/api/chatbot/messages", {
    method: "POST",

    headers: {
      "Content-Type": "application/json",
    },

    body: JSON.stringify({
      message: message,
    }),
  });

  if (response.status === 503) {
    const retryAfterSeconds = Number(response.headers.get("Retry-After")) || 5;

    throw new ChatbotUnavailableError(retryAfterSeconds);
  }

  if (!response.ok) {
    throw new Error(`Chatbot request failed: ${response.status}`);
  }

  return await response.json();
}

async function selectSuggestion(knowledgeItemId) {
  const response = await fetch("/api/chatbot/suggestions/select", {
    method: "POST",

    headers: {
      "Content-Type": "application/json",
    },

    body: JSON.stringify({
      knowledgeItemId: knowledgeItemId,
    }),
  });

  const responseText = await response.text();

  if (!response.ok) {
    throw new Error(
      `Suggestion request failed: ${response.status} - ${responseText}`,
    );
  }

  return JSON.parse(responseText);
}

function renderSuggestions(suggestions) {
  if (!suggestions || suggestions.length === 0) {
    return;
  }

  const container = document.createElement("div");

  container.classList.add("chatbot-suggestions");

  for (const suggestion of suggestions) {
    const button = document.createElement("button");

    button.type = "button";

    button.classList.add("chatbot-suggestion");

    button.textContent = suggestion.label;

    button.addEventListener("click", async () => {
      await handleSuggestionSelection(suggestion, container, button);
    });

    container.appendChild(button);
  }

  chatbotMessages.appendChild(container);

  scrollToBottom();
}

async function handleSuggestionSelection(
  suggestion,
  suggestionsContainer,
  selectedButton,
) {
  const buttons = suggestionsContainer.querySelectorAll("button");

  for (const button of buttons) {
    button.disabled = true;
  }

  selectedButton.classList.add("chatbot-suggestion--selected");

  // فقط بار اول انتخاب کاربر را نشان بده
  appendMessage(suggestion.label, "user");

  await processSuggestionRequest(suggestion, suggestionsContainer);
}

function handleChatbotResponse(result) {
  appendMessage(result.reply, "bot");

  if (result.responseType === "Suggestions") {
    renderSuggestions(result.suggestions);
  }
}

chatbotForm.addEventListener("submit", async (event) => {
  event.preventDefault();

  if (isSendingMessage) {
    return;
  }

  const message = chatbotInput.value.trim();

  if (!message) {
    return;
  }

  appendMessage(message, "user");

  chatbotInput.value = "";

  await processMessageRequest(message);
});

chatbotToggle.addEventListener("click", openChatbot);

chatbotClose.addEventListener("click", closeChatbot);

document.addEventListener("keydown", (event) => {
  if (event.key === "Escape" && !chatbotPanel.hidden) {
    closeChatbot();

    chatbotToggle.focus();
  }
});

function appendRetryMessage(text, retryAction) {
  const container = document.createElement("div");

  container.classList.add("chatbot-error");

  const message = document.createElement("div");

  message.classList.add("chatbot-message", "chatbot-message--bot");

  message.textContent = text;

  const retryButton = document.createElement("button");

  retryButton.type = "button";
  retryButton.classList.add("chatbot-retry");
  retryButton.textContent = "تلاش مجدد";

  retryButton.addEventListener("click", async () => {
    retryButton.disabled = true;

    container.remove();

    await retryAction();
  });

  container.appendChild(message);
  container.appendChild(retryButton);

  chatbotMessages.appendChild(container);

  scrollToBottom();
}

async function processMessageRequest(message) {
  if (isSendingMessage) {
    return;
  }

  isSendingMessage = true;

  setFormBusy(true);

  const loading = showLoading();

  try {
    const result = await sendMessage(message);

    loading.remove();

    handleChatbotResponse(result);
  } catch (error) {
    console.error(error);

    loading.remove();

    const errorMessage =
      error instanceof ChatbotUnavailableError
        ? "دستیار سایت در حال آماده‌سازی است. لطفاً چند لحظه دیگر دوباره تلاش کنید."
        : "در ارتباط با دستیار مشکلی پیش آمد.";

    appendRetryMessage(errorMessage, async () => {
      await processMessageRequest(message);
    });
  } finally {
    isSendingMessage = false;

    setFormBusy(false);

    chatbotInput.focus();
  }
}

async function processSuggestionRequest(suggestion, suggestionsContainer) {
  const loading = showLoading();

  try {
    const result = await selectSuggestion(suggestion.knowledgeItemId);

    loading.remove();

    suggestionsContainer.classList.add("chatbot-suggestions--answered");

    appendMessage(result.reply, "bot");
  } catch (error) {
    console.error(error);

    loading.remove();

    appendRetryMessage("در دریافت پاسخ مشکلی پیش آمد.", async () => {
      await processSuggestionRequest(suggestion, suggestionsContainer);
    });
  }
}
