document.getElementById('cultureSelect').addEventListener('change', function () {
    const culture = this.value;
    const returnUrl = window.location.pathname;

    fetch('/Language/SetLanguage', {
        method: 'POST',
        credentials: 'same-origin',
        headers: {
            'Content-Type': 'application/x-www-form-urlencoded'
        },
        body: new URLSearchParams({ culture, returnUrl })
    })
        .then(response => {
            if (response.ok) location.reload();
        });
});


const passwordInput = document.getElementById("password");
const icon = document.getElementById("eyeIcon");

icon.addEventListener("click", function () {
    passwordInput.type =
        passwordInput.type === "password" ? "text" : "password";

    icon.classList.toggle("bi-eye");
    icon.classList.toggle("bi-eye-slash");
});

function timeAgo(date) {
    const seconds = Math.floor((new Date() - new Date(date)) / 1000);
    if (seconds < 60) return seconds + " sec. ago";
    const minutes = Math.floor(seconds / 60);
    if (minutes < 60) return minutes + " min. ago";
    const hours = Math.floor(minutes / 60);
    if (hours < 24) return hours + " hours ago";
    const days = Math.floor(hours / 24);
    if (days < 7) return days + " days ago";
    if (days < 30) return Math.floor(days / 7) + " weeks ago";
    if (days < 365) return Math.floor(days / 30) + " months ago";
    return Math.floor(days / 365) + " years ago";
}

function updateTimeAgo() {
    document.querySelectorAll(".time-ago").forEach(el => {
        el.innerText = timeAgo(el.dataset.time);
    });
    document.querySelectorAll(".edited").forEach(el => {
        el.innerText = "(edited " + timeAgo(el.dataset.time) + ")";
    });
}

updateTimeAgo();

setInterval(updateTimeAgo, 60000);

const rules = {
    length: document.getElementById('rule-length'),
    uppercase: document.getElementById('rule-uppercase'),
    lowercase: document.getElementById('rule-lowercase'),
    number: document.getElementById('rule-number'),
    special: document.getElementById('rule-special')
};

const registerButton = document.getElementById('registerSubmit');
passwordInput.addEventListener('input', () => {
    const value = passwordInput.value;

    const validations = {
        length: value.length >= 5,
        uppercase: /[A-Z]/.test(value),
        lowercase: /[a-z]/.test(value),
        number: /[0-9]/.test(value),
        special: /[^a-zA-Z0-9]/.test(value)
    };

    let allValid = true;

    for (const [key, valid] of Object.entries(validations)) {
        if (valid) {
            rules[key].classList.add('text-success');
            rules[key].classList.remove('text-danger');
        } else {
            rules[key].classList.add('text-danger');
            rules[key].classList.remove('text-success');
            allValid = false;
        }
    }

    document.getElementById('password-rules').style.display = allValid ? 'none' : 'block';
    registerButton.disabled = !allValid;
});
