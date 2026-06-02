document.addEventListener("DOMContentLoaded", () => {
    const finishBtn = document.getElementById("finishBtn");

    function updateFinishButtonState() {
        if (!finishBtn) return;

        const forms = Array.from(document.querySelectorAll(".task-form"));
        finishBtn.disabled = forms.length > 0 && !forms.every(form => form._submitted);
    }

    function setResult(form, color, text) {
        const resultDiv = form.querySelector(".result");
        if (!resultDiv) return;

        resultDiv.style.color = color;
        resultDiv.textContent = text;
    }

    function lockForm(form) {
        form._submitted = true;
        form.querySelector(".check-btn")?.setAttribute("disabled", "disabled");
        updateFinishButtonState();
    }

    function clearMatchFeedback(form) {
        form.querySelectorAll(".left-item, .right-item")
            .forEach(el => el.style.background = "");
    }

    function clearTaskFeedback(form) {
        form.querySelector(".result")?.replaceChildren();

        if (form.classList.contains("match-form")) {
            clearMatchFeedback(form);
        }
    }

    document.querySelectorAll(".reorder-list").forEach(list => {
        let dragged = null;

        list.querySelectorAll("li").forEach(item => {
            item.setAttribute("draggable", "true");

            item.addEventListener("dragstart", () => {
                dragged = item;
            });

            item.addEventListener("dragover", e => e.preventDefault());

            item.addEventListener("drop", e => {
                e.preventDefault();

                if (!dragged || dragged === item) return;

                const items = Array.from(list.children);
                const draggedIndex = items.indexOf(dragged);
                const targetIndex = items.indexOf(item);

                if (draggedIndex < targetIndex) {
                    list.insertBefore(dragged, item.nextSibling);
                } else {
                    list.insertBefore(dragged, item);
                }
            });
        });
    });

    document.querySelectorAll(".match-form").forEach(form => {
        const leftItems = form.querySelectorAll(".left-item");
        const rightItems = form.querySelectorAll(".right-item");
        const svg = form.querySelector(".match-lines");

        form._pairs = [];
        let selectedLeft = null;

        leftItems.forEach(left => {
            left.addEventListener("click", () => {
                if (form._submitted) return;

                leftItems.forEach(l => l.classList.remove("selected"));
                left.classList.add("selected");
                selectedLeft = left;
            });
        });

        rightItems.forEach(right => {
            right.addEventListener("click", () => {
                if (form._submitted || !selectedLeft) return;

                const leftIndex = selectedLeft.dataset.index;
                const rightValue = right.dataset.value;

                form._pairs = form._pairs.filter(p => p.LeftIndex !== leftIndex);

                if (form._pairs.some(p => p.RightValue === rightValue)) return;

                form._pairs.push({
                    LeftIndex: leftIndex,
                    RightValue: rightValue
                });

                clearMatchFeedback(form);
                drawLines(form);

                selectedLeft.classList.remove("selected");
                selectedLeft = null;
            });
        });

        form._drawLines = colorMap => drawLines(form, colorMap);

        if (svg) {
            window.addEventListener("resize", () => drawLines(form));
        }
    });

    function drawLines(form, colorMap = null) {
        const svg = form.querySelector(".match-lines");
        if (!svg) return;

        svg.innerHTML = "";

        (form._pairs || []).forEach(pair => {
            const left = form.querySelector(`.left-item[data-index='${pair.LeftIndex}']`);
            const right = form.querySelector(`.right-item[data-value='${pair.RightValue}']`);

            if (!left || !right) return;

            const leftRect = left.getBoundingClientRect();
            const rightRect = right.getBoundingClientRect();
            const svgRect = svg.getBoundingClientRect();

            const line = document.createElementNS("http://www.w3.org/2000/svg", "line");

            line.setAttribute("x1", leftRect.right - svgRect.left);
            line.setAttribute("y1", leftRect.top + leftRect.height / 2 - svgRect.top);
            line.setAttribute("x2", rightRect.left - svgRect.left);
            line.setAttribute("y2", rightRect.top + rightRect.height / 2 - svgRect.top);
            line.setAttribute("stroke-width", "2");
            line.setAttribute("stroke", colorMap
                ? (colorMap[pair.LeftIndex] ? "#4caf50" : "#e53935")
                : "#555");

            svg.appendChild(line);
        });
    }

    document.querySelectorAll(".task-form").forEach(form => {
        form.addEventListener("submit", async event => {
            event.preventDefault();
            clearTaskFeedback(form);

            const reorderList = form.querySelector(".reorder-list");
            if (reorderList) {
                form.querySelector(".order-input").value = Array.from(reorderList.children)
                    .map(item => item.dataset.index)
                    .join(",");
            }

            if (form.classList.contains("match-form")) {
                const pairs = form._pairs || [];
                const leftCount = form.querySelectorAll(".left-item").length;

                if (pairs.length !== leftCount) {
                    setResult(form, "orange", "Зістав усі пари.");
                    return;
                }

                form.querySelector(".pairs-input").value = JSON.stringify(pairs);
            }

            try {
                const response = await fetch(form.action, {
                    method: "POST",
                    body: new FormData(form)
                });

                const text = await response.text();

                if (!response.ok) {
                    setResult(form, "orange", text);
                    return;
                }

                const data = JSON.parse(text);

                if (data.isCorrect) {
                    setResult(form, "green", "Правильно");
                    lockForm(form);
                } else {
                    const attemptsLeft = Number(data.attemptsLeft ?? 0);

                    setResult(form, "red", attemptsLeft > 0
                        ? `Неправильно. Залишилось спроб: ${attemptsLeft}`
                        : "Неправильно. Спроби вичерпано");

                    if (attemptsLeft <= 0) {
                        lockForm(form);
                    }
                }

                if (form.classList.contains("match-form")) {
                    highlightMatch(form, data.isCorrect, data.pairResults || []);
                }

                if (reorderList) {
                    highlightReorder(form, data.isCorrect, data.correctOrder);
                }

                updateFinishButtonState();
            } catch {
                setResult(form, "red", "Помилка запиту");
            }
        });
    });

    function highlightMatch(form, isCorrect, pairResults) {
        clearMatchFeedback(form);

        const colorMap = {};

        (form._pairs || []).forEach(pair => {
            const result = pairResults.find(item =>
                String(item.leftIndex ?? item.LeftIndex) === String(pair.LeftIndex) &&
                String(item.rightValue ?? item.RightValue) === String(pair.RightValue));

            const pairIsCorrect = isCorrect ||
                result?.isCorrect === true ||
                result?.IsCorrect === true;

            colorMap[pair.LeftIndex] = pairIsCorrect;

            const color = pairIsCorrect ? "#c8f7c5" : "#f7c5c5";
            const left = form.querySelector(`.left-item[data-index='${pair.LeftIndex}']`);
            const right = form.querySelector(`.right-item[data-value='${pair.RightValue}']`);

            if (left) left.style.background = color;
            if (right) right.style.background = color;
        });

        form._drawLines?.(colorMap);
    }

    function highlightReorder(form, isCorrect, correctOrder) {
        const list = form.querySelector(".reorder-list");
        if (!list) return;

        const items = Array.from(list.children);

        if (isCorrect) {
            items.forEach(item => item.style.background = "#c8f7c5");
            return;
        }

        const order = Array.isArray(correctOrder)
            ? correctOrder.map(String)
            : String(correctOrder || "").split(",");

        items.forEach((item, index) => {
            const isItemCorrect = String(item.dataset.index) === String(order[index]);
            item.style.background = isItemCorrect ? "#c8f7c5" : "#f7c5c5";
        });
    }

    updateFinishButtonState();
});

(function () {
    "use strict";

    document.addEventListener("DOMContentLoaded", function () {
        var fill = document.querySelector(".lessons-progress-fill");
        if (!fill) return;

        var target = parseFloat(fill.dataset.pct) || 0;

        fill.style.width = "0%";

        requestAnimationFrame(() => {
            requestAnimationFrame(() => {
                fill.style.width = target + "%";
            });
        });
    });
})();

document.getElementById("finishBtn")?.addEventListener("click", async () => {
    const attemptId = document.getElementById("attemptIdGlobal").value;

    const formData = new FormData();
    formData.append("attemptId", attemptId);

    try {
        const response = await fetch("/ExercisePlay/Finish", {
            method: "POST",
            body: formData
        });

        const data = await response.json();

        document.querySelectorAll("button").forEach(button => {
            button.disabled = true;
        });

        const resultDiv = document.getElementById("finalResult");

        resultDiv.innerHTML = `
            <div class="content-panel text-center">
                <h3 class="mb-2">Результат</h3>
                <p class="metric-value">${data.correct} / ${data.total}</p>
                <div class="action-row justify-content-center mt-3">
                    <button class="btn btn-outline-primary" onclick="location.reload()">
                        Пройти ще раз
                    </button>
                    <button class="btn btn-primary" onclick="window.location.href='/ExercisePlay'">
                        До списку вправ
                    </button>
                </div>
            </div>
        `;
    } catch {
        alert("Помилка при завершенні");
    }
});
