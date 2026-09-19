import { css, html } from "@umbraco-cms/backoffice/external/lit";

import { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";

import { UMB_AUTH_CONTEXT } from "@umbraco-cms/backoffice/auth";

function registerDashboardFont() {
  const styleId = "drs-consult-requests-font";

  if (document.getElementById(styleId)) {
    return;
  }

  const style = document.createElement("style");

  style.id = styleId;

  style.textContent = `
    @font-face {
        font-family: "YekanBakhFaNum";
        src:
            url("/App_Plugins/ConsultRequests/fonts/YekanBakhFaNum-Regular.woff2")
            format("woff2");
        font-style: normal;
        font-weight: 100 900;
        font-display: swap;
    }
`;

  document.head.appendChild(style);
}

function findElementByText(root, expectedText) {
  const elements = root.querySelectorAll("*");

  for (const element of elements) {
    const directText = Array.from(element.childNodes)
      .filter((node) => node.nodeType === Node.TEXT_NODE)
      .map((node) => node.textContent?.trim() ?? "")
      .join("")
      .trim();

    if (directText === expectedText) {
      return element;
    }

    if (element.shadowRoot) {
      const shadowResult = findElementByText(element.shadowRoot, expectedText);

      if (shadowResult) {
        return shadowResult;
      }
    }
  }

  return null;
}

function applyDashboardTabFont(attemptNumber = 0) {
  const tabLabel = findElementByText(document, "درخواست‌های همکاری");

  if (!tabLabel) {
    if (attemptNumber < 10) {
      window.setTimeout(() => applyDashboardTabFont(attemptNumber + 1), 150);
    }

    return;
  }

  tabLabel.style.setProperty(
    "font-family",
    '"YekanBakhFaNum", Tahoma, sans-serif',
    "important",
  );

  tabLabel.style.setProperty("font-weight", "400", "important");
}

const applicationsApiUrl =
  "/umbraco/management/api/v1/consult-requests/job-applications?take=50";

export class JobApplicationsDashboardElement extends UmbLitElement {
  static properties = {
    _applications: {
      state: true,
    },

    _isLoading: {
      state: true,
    },

    _errorMessage: {
      state: true,
    },

    _downloadingId: {
      state: true,
    },
  };

  static styles = css`
    :host {
      display: block;
      direction: rtl;
      color: #182035;
      font-family: "YekanBakhFaNum", Tahoma, sans-serif;
    }

    .dashboard,
    .dashboard *,
    button,
    table,
    th,
    td {
      font-family: "YekanBakhFaNum", Tahoma, sans-serif;
    }

    .dashboard {
      padding: 24px;
      font-family: inherit;
    }

    .header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 16px;
      margin-bottom: 20px;
    }

    .header h2 {
      margin: 0;
      font-family: inherit;
      font-size: 20px;
      font-weight: 700;
    }

    .refresh-button,
    .download-button {
      display: inline-flex;
      align-items: center;
      justify-content: center;

      height: 38px;
      border: 0;
      border-radius: 7px;
      padding: 0 16px;

      color: #fff;
      font-family: inherit;
      font-size: 14px;
      font-weight: 500;
      line-height: 1;

      white-space: nowrap;
      cursor: pointer;

      transition:
        background-color 160ms ease,
        box-shadow 160ms ease,
        opacity 160ms ease;
    }

    .refresh-button {
      min-width: 96px;
      background-color: #1b2855;
    }

    .refresh-button:hover:not(:disabled) {
      background-color: #263873;
      box-shadow: 0 3px 10px rgb(27 40 85 / 18%);
    }

    .download-button {
      width: 128px;
      min-width: 128px;
      background-color: #006eff;
    }

    .download-button:hover:not(:disabled) {
      background-color: #005bd6;
      box-shadow: 0 3px 10px rgb(0 110 255 / 20%);
    }

    .refresh-button:focus-visible,
    .download-button:focus-visible {
      outline: 3px solid rgb(0 110 255 / 25%);
      outline-offset: 2px;
    }

    .refresh-button:disabled,
    .download-button:disabled {
      cursor: wait;
      opacity: 0.65;
    }

    .table-container {
      width: 100%;
      overflow-x: auto;
      border: 1px solid #e6e8ec;
      border-radius: 8px;
    }

    table {
      width: 100%;
      border-collapse: collapse;
      background-color: #fff;
      font-family: inherit;
    }

    th,
    td {
      padding: 13px 14px;
      text-align: right;
      vertical-align: middle;
      border-bottom: 1px solid #e8e9ed;
    }

    th {
      color: #242b3e;
      font-weight: 600;
      white-space: nowrap;
      background-color: #f4f5f7;
    }

    tbody tr {
      transition: background-color 140ms ease;
    }

    tbody tr:hover {
      background-color: #f8faff;
    }

    tbody tr:last-child td {
      border-bottom: 0;
    }

    th:last-child,
    td:last-child {
      width: 144px;
      min-width: 144px;
      text-align: center;
    }

    .message-column {
      min-width: 220px;
      max-width: 360px;
      white-space: pre-wrap;
      overflow-wrap: anywhere;
    }

    .no-resume {
      color: #7b8190;
    }

    .box-title {
      margin: -4px 0 20px;
      padding: 0 0 16px;

      color: #182035;
      font-family: "YekanBakhFaNum", Tahoma, sans-serif;
      font-size: 14px;
      font-weight: 500;
      text-align: right;

      border-bottom: 1px solid #e5e7eb;
    }
  `;

  constructor() {
    super();

    this._applications = [];
    this._isLoading = true;
    this._errorMessage = "";
    this._downloadingId = null;
  }

  connectedCallback() {
    super.connectedCallback();

    registerDashboardFont();
    applyDashboardTabFont();

    void this.loadApplications();
  }

  firstUpdated() {
    applyDashboardTabFont();
  }

  async getAccessToken() {
    const authContext = await this.getContext(UMB_AUTH_CONTEXT);

    if (!authContext) {
      throw new Error("اطلاعات احراز هویت Backoffice در دسترس نیست.");
    }

    const token = await authContext.getLatestToken();

    if (!token) {
      throw new Error("نشست کاربری معتبر نیست. دوباره وارد Backoffice شوید.");
    }

    return token;
  }

  async loadApplications() {
    this._isLoading = true;
    this._errorMessage = "";

    try {
      const token = await this.getAccessToken();

      const response = await fetch(applicationsApiUrl, {
        method: "GET",

        headers: {
          Accept: "application/json",
          Authorization: `Bearer ${token}`,
        },

        credentials: "include",
        cache: "no-store",
      });

      if (!response.ok) {
        throw new Error(this.getHttpErrorMessage(response.status));
      }

      const result = await response.json();

      this._applications = Array.isArray(result) ? result : [];
    } catch (error) {
      console.error("Could not load job applications.", error);

      this._errorMessage =
        error instanceof Error
          ? error.message
          : "دریافت درخواست‌های همکاری با خطا مواجه شد.";
    } finally {
      this._isLoading = false;
    }
  }

  async downloadResume(applicationId) {
    this._errorMessage = "";
    this._downloadingId = applicationId;

    try {
      const token = await this.getAccessToken();

      const response = await fetch(
        `/umbraco/management/api/v1/consult-requests/job-applications/${applicationId}/resume`,
        {
          method: "GET",

          headers: {
            Accept: "application/pdf",
            Authorization: `Bearer ${token}`,
          },

          credentials: "include",
          cache: "no-store",
        },
      );

      if (!response.ok) {
        throw new Error(this.getHttpErrorMessage(response.status));
      }

      const resumeBlob = await response.blob();

      const objectUrl = URL.createObjectURL(resumeBlob);

      const downloadLink = document.createElement("a");

      downloadLink.href = objectUrl;

      downloadLink.download = `resume-${applicationId}.pdf`;

      downloadLink.click();

      window.setTimeout(() => URL.revokeObjectURL(objectUrl), 1000);
    } catch (error) {
      console.error("Could not download resume.", error);

      this._errorMessage =
        error instanceof Error
          ? error.message
          : "دانلود رزومه با خطا مواجه شد.";
    } finally {
      this._downloadingId = null;
    }
  }

  getHttpErrorMessage(statusCode) {
    switch (statusCode) {
      case 401:
        return "نشست کاربری منقضی شده است. دوباره وارد Backoffice شوید.";

      case 403:
        return "شما اجازه مشاهده درخواست‌های همکاری را ندارید.";

      case 404:
        return "درخواست یا فایل رزومه پیدا نشد.";

      default:
        return `عملیات با خطا مواجه شد. کد پاسخ: ${statusCode}`;
    }
  }

  formatDate(value) {
    if (!value) {
      return "—";
    }

    const date = new Date(value);

    if (Number.isNaN(date.getTime())) {
      return value;
    }

    return new Intl.DateTimeFormat("fa-IR", {
      dateStyle: "medium",
      timeStyle: "short",
    }).format(date);
  }

  render() {
    return html`
      <div class="dashboard">
        <uui-box>
          <div class="box-title">درخواست‌های همکاری</div>
          <div class="header">
            <h2>آخرین درخواست‌ها</h2>

            <button
              type="button"
              class="refresh-button"
              ?disabled=${this._isLoading}
              @click=${() => this.loadApplications()}
            >
              ${this._isLoading ? "در حال دریافت..." : "به‌روزرسانی"}
            </button>
          </div>

          ${this._errorMessage
            ? html`
                <div class="message error-message">${this._errorMessage}</div>
              `
            : ""}
          ${this._isLoading
            ? html`
                <div class="message loading-message">
                  در حال دریافت درخواست‌های همکاری...
                </div>
              `
            : this.renderApplications()}
        </uui-box>
      </div>
    `;
  }

  renderApplications() {
    if (this._applications.length === 0) {
      return html`
        <div class="message empty-message">
          هنوز درخواست همکاری ثبت نشده است.
        </div>
      `;
    }

    return html`
      <div class="table-container">
        <table>
          <thead>
            <tr>
              <th>شناسه</th>
              <th>تاریخ ثبت</th>
              <th>نام و نام خانوادگی</th>
              <th>شماره موبایل</th>
              <th>موقعیت شغلی</th>
              <th>توضیحات</th>
              <th>رزومه</th>
            </tr>
          </thead>

          <tbody>
            ${this._applications.map((application) =>
              this.renderApplicationRow(application),
            )}
          </tbody>
        </table>
      </div>
    `;
  }

  renderApplicationRow(application) {
    const isDownloading = this._downloadingId === application.id;

    return html`
      <tr>
        <td>${application.id}</td>

        <td>${this.formatDate(application.createdAt)}</td>

        <td>${application.fullName || "—"}</td>

        <td>${application.mobile || "—"}</td>

        <td>${application.requestType || "—"}</td>

        <td class="message-column">${application.message || "—"}</td>

        <td>
          ${application.hasResume
            ? html`
                <button
                  type="button"
                  class="download-button"
                  ?disabled=${isDownloading}
                  @click=${() => this.downloadResume(application.id)}
                >
                  ${isDownloading ? "در حال دانلود..." : "دانلود رزومه"}
                </button>
              `
            : html` <span class="no-resume"> بدون رزومه </span> `}
        </td>
      </tr>
    `;
  }
}

if (!customElements.get("drs-job-applications-dashboard")) {
  customElements.define(
    "drs-job-applications-dashboard",
    JobApplicationsDashboardElement,
  );
}
