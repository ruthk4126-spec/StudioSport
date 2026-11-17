const API_BASE="/api";
//  = "http://localhost:5170/api";
const TOKEN_KEY = "studio_token";
const USER_KEY = "studio_user";

/* ---------- ניהול טוקן ---------- */

function saveSession(token, user) {
  localStorage.setItem(TOKEN_KEY, token);
  localStorage.setItem(USER_KEY, JSON.stringify(user || {}));
}

function getToken() {
  return localStorage.getItem(TOKEN_KEY);
}

function getUser() {
  try {
    return JSON.parse(localStorage.getItem(USER_KEY) || "{}");
  } catch {
    return {};
  }
}

function clearSession() {
  localStorage.removeItem(TOKEN_KEY);
  localStorage.removeItem(USER_KEY);
}

function requireAuth() {
  if (!getToken()) {
    window.location.href = "login.html";
  }
}

/* ---------- קריאה כללית ---------- */

async function apiRequest(path, options = {}) {
  const headers = {
    "Content-Type": "application/json",
    ...(options.headers || {})
  };

  const token = getToken();
  if (token) headers["Authorization"] = "Bearer " + token;

  const response = await fetch(API_BASE + path, {
    ...options,
    headers
  });

  if (response.status === 401) {
    clearSession();
    window.location.href = "login.html";
    throw new Error("פג תוקף ההתחברות");
  }

  let data = null;
  const text = await response.text();
  if (text) {
    try {
      data = JSON.parse(text);
    } catch {
      data = text;
    }
  }

  return {
    ok: response.ok,
    status: response.status,
    data,
    correlationId: response.headers.get("X-Correlation-Id")
  };
}

/* ---------- שירותים ---------- */

const Api = {
  login(username, password) {
    return apiRequest("/auth/login", {
      method: "POST",
      body: JSON.stringify({ username, password })
    });
  },

  register(username, password, email) {
    return apiRequest("/auth/register", {
      method: "POST",
      body: JSON.stringify({ username, password, email })
    });
  },

  me() {
    return apiRequest("/auth/me");
  },

  getStats() {
    return apiRequest("/classes/stats");
  },

  getClasses() {
    return apiRequest("/classes");
  },

  getClass(id) {
    return apiRequest("/classes/" + id);
  },

  enroll(classId) {
    return apiRequest(`/classes/${classId}/enrollments`, {
      method: "POST",
      body: JSON.stringify({})
    });
  },

  cancelEnrollment(enrollmentId) {
    return apiRequest(`/enrollments/${enrollmentId}`, { method: "DELETE" });
  },

  getMyEnrollments() {
    return apiRequest("/enrollments/my");
  },
  getMyPending() {
    return apiRequest("/subscriptions/my-pending");
  },

 addFeedback(enrollmentId, rating, comment) {
    return apiRequest(`/enrollments/${enrollmentId}/feedback`, {
      method: "POST",
      body: JSON.stringify({ rating, comment })
    });
  },

  getMySubscription() {
    return apiRequest("/subscriptions/my");
  },

  getPlans() {
    return apiRequest("/subscriptions/plans");
  },

  requestSubscription(plan) {
    return apiRequest("/subscriptions/request?plan=" + plan, { method: "POST" });
  }
  ,getPendingSubscriptions: () => apiRequest('/subscriptions/pending'),
  
approveSubscription: (id) => apiRequest(`/subscriptions/${id}/approve`, { method: 'POST' }),
};


/* ---------- עזרי תצוגה ---------- */

function formatDate(iso) {
  if (!iso) return "";
  const d = new Date(iso);
  return d.toLocaleDateString("he-IL", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric"
  }) + " · " + d.toLocaleTimeString("he-IL", {
    hour: "2-digit",
    minute: "2-digit"
  });
}

function renderUserArea() {
  const user = getUser();
  const nameEl = document.getElementById("userName");
  const avatarEl = document.getElementById("userAvatar");
  if (nameEl) nameEl.textContent = user.username || "אורח";
  if (avatarEl) avatarEl.textContent = (user.username || "?").charAt(0).toUpperCase();
}

function logout() {
  clearSession();
  window.location.href = "login.html";
}

/* ---------- הודעות ---------- */

function showAlert(elementId, message, type) {
  const el = document.getElementById(elementId);
  if (!el) return;
  el.textContent = message;
  el.className = "alert show " + (type === "success" ? "alert-success" : "alert-error");
}

function hideAlert(elementId) {
  const el = document.getElementById(elementId);
  if (el) el.className = "alert";
}

function escapeHtml(str) {
  return String(str || "").replace(/[&<>"']/g, ch => ({
    "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;"
  }[ch]));
}

/* ---------- נרמול תשובות מהשרת ---------- */

// השרת עשוי להחזיר מערך ישירות, או עטיפה: { items: [...] } / { data: [...] }
function asArray(payload) {
  if (Array.isArray(payload)) return payload;
  if (payload && Array.isArray(payload.items)) return payload.items;
  if (payload && Array.isArray(payload.data)) return payload.data;
  return [];
}

// ה-enum נשלח כמחרוזת ("Active") או כמספר (0)
function isActiveStatus(status) {
  return status === "Active" || status === 0;
}

// שם השדה עשוי להיות classId או classSessionId
function enrollmentClassId(e) {
  return e.classId ?? e.classSessionId ?? e.classSession?.id ?? null;
} 
Api.getTrainers = () => apiRequest("/trainers");
Api.createClass = (payload) => apiRequest("/classes", { method: "POST", body: JSON.stringify(payload) });
Api.cancelClass = (id) => apiRequest("/classes/" + id, { method: "DELETE" });
