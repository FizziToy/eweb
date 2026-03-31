document.querySelectorAll(".match-form").forEach(form => {

    let selectedLeft = null;
    let pairs = [];

    const leftItems = form.querySelectorAll(".left-item");
    const rightItems = form.querySelectorAll(".right-item");

    leftItems.forEach(left => {
        left.addEventListener("click", () => {
            selectedLeft = left;

            leftItems.forEach(x => x.classList.remove("selected"));
            left.classList.add("selected");
        });
    });

    rightItems.forEach(right => {
        right.addEventListener("click", () => {
            if (!selectedLeft) return;

            pairs.push({
                leftIndex: selectedLeft.dataset.index,
                rightValue: right.dataset.value
            });

            selectedLeft.classList.remove("selected");
            selectedLeft.classList.add("matched");
            right.classList.add("matched");

            selectedLeft = null;
        });
    });

    form.addEventListener("submit", async function (e) {
        e.preventDefault();

        form.querySelector(".pairs-input").value = JSON.stringify(pairs);

        const formData = new FormData(form);

        const response = await fetch(form.action, {
            method: "POST",
            body: formData
        });

        const resultDiv = form.querySelector(".result");

        if (!response.ok) {
            resultDiv.innerText = "❌ Помилка";
            return;
        }

        const data = await response.json();

        resultDiv.innerText = data.isCorrect
            ? "✅ Правильно"
            : "❌ Неправильно";
    });

});