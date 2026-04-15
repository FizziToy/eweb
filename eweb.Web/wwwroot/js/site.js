document.addEventListener("DOMContentLoaded", () => {

    // =========================
    // REORDER INIT
    // =========================
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

                if (dragged && dragged !== item) {
                    const items = Array.from(list.children);
                    const draggedIndex = items.indexOf(dragged);
                    const targetIndex = items.indexOf(item);

                    if (draggedIndex < targetIndex) {
                        list.insertBefore(dragged, item.nextSibling);
                    } else {
                        list.insertBefore(dragged, item);
                    }
                }
            });
        });
    });

    // =========================
    // MATCH INIT
    // =========================
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

                if (form._submitted) return;
                if (!selectedLeft) return;

                const leftIndex = selectedLeft.dataset.index;
                const rightValue = right.dataset.value;

                // remove old pair
                form._pairs = form._pairs.filter(p => p.LeftIndex !== leftIndex);

                // prevent duplicate right
                if (form._pairs.some(p => p.RightValue === rightValue)) return;

                form._pairs.push({
                    LeftIndex: leftIndex,
                    RightValue: rightValue
                });

                drawLines();

                selectedLeft.classList.remove("selected");
                selectedLeft = null;
            });
        });

        function drawLines(colorMap = null) {
            svg.innerHTML = "";

            form._pairs.forEach(p => {

                const left = form.querySelector(`.left-item[data-index='${p.LeftIndex}']`);
                const right = form.querySelector(`.right-item[data-value='${p.RightValue}']`);

                if (!left || !right) return;

                const lRect = left.getBoundingClientRect();
                const rRect = right.getBoundingClientRect();
                const sRect = svg.getBoundingClientRect();

                const x1 = lRect.right - sRect.left;
                const y1 = lRect.top + lRect.height / 2 - sRect.top;

                const x2 = rRect.left - sRect.left;
                const y2 = rRect.top + rRect.height / 2 - sRect.top;

                const line = document.createElementNS("http://www.w3.org/2000/svg", "line");

                line.setAttribute("x1", x1);
                line.setAttribute("y1", y1);
                line.setAttribute("x2", x2);
                line.setAttribute("y2", y2);
                line.setAttribute("stroke-width", "2");

                const color = colorMap
                    ? (colorMap[p.LeftIndex] ? "#4caf50" : "#e53935")
                    : "#555";

                line.setAttribute("stroke", color);

                svg.appendChild(line);
            });
        }

        form._drawLines = drawLines;
    });

    // =========================
    // SUBMIT (ЄДИНИЙ)
    // =========================
    document.querySelectorAll(".task-form").forEach(form => {

        let attempts = 0;

        form.addEventListener("submit", async function (e) {
            e.preventDefault();

            const resultDiv = form.querySelector(".result");
            resultDiv.textContent = "";

            // ---------- REORDER ----------
            const reorderList = form.querySelector(".reorder-list");
            if (reorderList) {
                const order = Array.from(reorderList.children)
                    .map(li => li.dataset.index)
                    .join(",");

                form.querySelector(".order-input").value = order;
            }

            // ---------- MATCH ----------
            if (form.classList.contains("match-form")) {

                const pairs = form._pairs || [];
                const leftCount = form.querySelectorAll(".left-item").length;

                if (pairs.length !== leftCount) {
                    alert("Зістав всі пари!");
                    return;
                }

                form.querySelector(".pairs-input").value = JSON.stringify(pairs);
            }

            // ---------- SEND ----------
            const formData = new FormData(form);

            try {
                const response = await fetch(form.action, {
                    method: "POST",
                    body: formData
                });

                const text = await response.text();

                if (!response.ok) {
                    resultDiv.style.color = "orange";
                    resultDiv.textContent = text;
                    return;
                }

                const data = JSON.parse(text);

                attempts++;

                if (data.isCorrect) {
                    resultDiv.style.color = "green";
                    resultDiv.textContent = "✔ Правильно";
                    form._submitted = true;
                    form.querySelector(".check-btn").disabled = true;
                } else {
                    resultDiv.style.color = "red";
                    resultDiv.textContent = "✖ Неправильно";
                }

                if (attempts >= 2) {
                    form.querySelector(".check-btn").disabled = true;
                    form._submitted = true;
                }

                // ---------- MATCH HIGHLIGHT ----------
                if (form.classList.contains("match-form")) {
                    highlightMatch(form, data.isCorrect, data.correctPairs);
                }

                // ---------- REORDER HIGHLIGHT ----------
                if (reorderList) {
                    highlightReorder(form, data.isCorrect, data.correctOrder);
                }

            } catch {
                resultDiv.style.color = "red";
                resultDiv.textContent = "Помилка запиту";
            }
        });
    });

    // =========================
    // MATCH HIGHLIGHT
    // =========================
    function highlightMatch(form, isCorrect, correctPairs) {

        form.querySelectorAll(".left-item, .right-item")
            .forEach(el => el.style.background = "");

        const pairs = form._pairs || {};
        const colorMap = {};

        pairs.forEach(p => {

            const ok = isCorrect || correctPairs?.some(
                c => String(c.LeftIndex) === String(p.LeftIndex) &&
                    String(c.RightValue) === String(p.RightValue)
            );

            colorMap[p.LeftIndex] = ok;

            const color = ok ? "#c8f7c5" : "#f7c5c5";

            form.querySelector(`.left-item[data-index='${p.LeftIndex}']`).style.background = color;
            form.querySelector(`.right-item[data-value='${p.RightValue}']`).style.background = color;
        });

        form._drawLines?.(colorMap);
    }

    // =========================
    // REORDER HIGHLIGHT
    // =========================
    function highlightReorder(form, isCorrect, correctOrder) {

        const list = form.querySelector(".reorder-list");
        if (!list) return;

        const items = Array.from(list.children);

        if (isCorrect) {
            items.forEach(li => li.style.background = "#c8f7c5");
            return;
        }

        const order = Array.isArray(correctOrder)
            ? correctOrder.map(String)
            : String(correctOrder || "").split(",");

        items.forEach((li, i) => {
            const correct = String(li.dataset.index) === String(order[i]);
            li.style.background = correct ? "#c8f7c5" : "#f7c5c5";
        });
    }

});