document.addEventListener("DOMContentLoaded", () => {
  const form = document.getElementById("editAnswersForm");
  if (!form) return;
  const previewUrl = form.dataset.previewUrl;
  const submitBtn = document.getElementById("submitEditBtn");
  const flipModal = new bootstrap.Modal(document.getElementById("flipConfirmModal"));
  const flipBody = document.getElementById("flipModalBody");
  const flipConfirmBtn = document.getElementById("flipConfirmBtn");

  const initialState = {};
  document.querySelectorAll(".question-card").forEach(card => {
    const qid = card.dataset.questionId;
    const type = card.dataset.questionType;
    if (type === "Essay") return;
    const checked = Array.from(card.querySelectorAll(".answer-input:checked"))
      .map(i => i.value).sort();
    initialState[qid] = { type, options: checked };
  });

  function getCurrentAnswers(card) {
    const qid = card.dataset.questionId;
    const type = card.dataset.questionType;
    if (type === "Essay") return null;
    const checked = Array.from(card.querySelectorAll(".answer-input:checked"))
      .map(i => i.value).sort();
    return { qid, type, options: checked };
  }

  function isDirty(card) {
    const cur = getCurrentAnswers(card);
    if (!cur) return false;
    const init = initialState[cur.qid];
    if (!init) return false;
    if (cur.options.length !== init.options.length) return true;
    return cur.options.some((v, i) => v !== init.options[i]);
  }

  function updateDirtyUI(card) {
    const reasonBlock = card.querySelector(".reason-block");
    if (!reasonBlock) return;
    if (isDirty(card)) {
      card.classList.add("border-warning");
      reasonBlock.classList.remove("d-none");
    } else {
      card.classList.remove("border-warning");
      reasonBlock.classList.add("d-none");
      const select = reasonBlock.querySelector(".reason-code");
      const textarea = reasonBlock.querySelector(".reason-text");
      if (select) select.value = "";
      if (textarea) { textarea.value = ""; textarea.classList.add("d-none"); }
    }
  }

  document.querySelectorAll(".answer-input").forEach(input => {
    input.addEventListener("change", () => {
      const card = input.closest(".question-card");
      updateDirtyUI(card);
    });
  });

  document.querySelectorAll(".reason-code").forEach(sel => {
    sel.addEventListener("change", () => {
      const block = sel.closest(".reason-block");
      const ta = block.querySelector(".reason-text");
      if (sel.value === "Lainnya") {
        ta.classList.remove("d-none");
      } else {
        ta.classList.add("d-none");
        ta.value = "";
      }
    });
  });

  function collectDiff() {
    const diff = [];
    document.querySelectorAll(".question-card").forEach(card => {
      if (!isDirty(card)) return;
      const qid = card.dataset.questionId;
      const cur = getCurrentAnswers(card);
      const reasonBlock = card.querySelector(".reason-block");
      const code = reasonBlock.querySelector(".reason-code").value;
      const text = reasonBlock.querySelector(".reason-text").value;
      diff.push({ questionId: qid, options: cur.options.map(Number), reasonCode: code, reasonText: text });
    });
    return diff;
  }

  function validateClient(diff) {
    if (diff.length === 0) {
      alert("Tidak ada perubahan untuk disimpan.");
      return false;
    }
    for (const d of diff) {
      if (!d.reasonCode) {
        alert(`Pilih alasan edit terlebih dahulu (Soal #${d.questionId}).`);
        return false;
      }
      if (d.reasonCode === "Lainnya" && !d.reasonText.trim()) {
        alert(`Isi detail alasan untuk opsi Lainnya (Soal #${d.questionId}).`);
        return false;
      }
    }
    return true;
  }

  form.addEventListener("submit", async (e) => {
    e.preventDefault();
    const diff = collectDiff();
    if (!validateClient(diff)) return;

    submitBtn.disabled = true;
    submitBtn.textContent = "Memeriksa...";

    try {
      const fd = new FormData();
      diff.forEach((d, i) => {
        fd.append(`Drafts[${i}].QuestionId`, d.questionId);
        d.options.forEach(o => fd.append(`Drafts[${i}].Options`, o));
      });
      const resp = await fetch(previewUrl, {
        method: "POST",
        body: fd,
        headers: {
          "RequestVerificationToken": form.querySelector('input[name=__RequestVerificationToken]').value
        }
      });
      if (!resp.ok) throw new Error("Preview gagal");
      const preview = await resp.json();
      const oldPassed = preview.oldIsPassed;
      const newPassed = preview.newIsPassed;
      const flip = (oldPassed === true && newPassed === false) || (oldPassed === false && newPassed === true);

      if (flip) {
        let msg;
        if (oldPassed === true && newPassed === false) {
          msg = `Perubahan ini akan <strong>menggagalkan peserta</strong>. ` +
                `NomorSertifikat akan dicabut${preview.nomorSertifikat ? ` (No: ${preview.nomorSertifikat})` : ""} ` +
                `dan TrainingRecord di-set Failed. Lanjutkan?`;
        } else {
          msg = `Perubahan ini akan <strong>meluluskan peserta</strong>. ` +
                (preview.willGenerateCert
                  ? `NomorSertifikat baru akan di-generate (GenerateCertificate && bukan PreTest).`
                  : `Sertifikat TIDAK akan di-generate (session bukan eligible).`) +
                ` Lanjutkan?`;
        }
        flipBody.innerHTML = msg;
        flipConfirmBtn.onclick = () => { flipModal.hide(); form.submit(); };
        flipModal.show();
        submitBtn.disabled = false;
        submitBtn.textContent = "Save & Recompute";
        document.getElementById('flipConfirmModal').addEventListener('hidden.bs.modal', function once() {
          submitBtn.focus();
          document.getElementById('flipConfirmModal').removeEventListener('hidden.bs.modal', once);
        });
        return;
      }

      form.submit();
    } catch (err) {
      console.error(err);
      alert("Gagal memeriksa preview: " + err.message);
      submitBtn.disabled = false;
      submitBtn.textContent = "Save & Recompute";
    }
  });
});
