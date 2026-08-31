(() => {
  function handleRouteChange() {
    const path = window.location.pathname.toLowerCase();

    if (path === "/login") {
      window.location.replace("/signout");
    }
  }

  const originalPushState = history.pushState;

  history.pushState = function (...args) {
    const result = originalPushState.apply(this, args);

    handleRouteChange();

    return result;
  };

  const originalReplaceState = history.replaceState;

  history.replaceState = function (...args) {
    const result = originalReplaceState.apply(this, args);

    handleRouteChange();

    return result;
  };

  window.addEventListener("popstate", handleRouteChange);

  handleRouteChange();
})();
