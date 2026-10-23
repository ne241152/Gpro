mergeInto(LibraryManager.library, {
  GproRequestPasskeyAuth: function () {
    if (typeof window.GproShowPasskeyAuth === "function") {
      window.GproShowPasskeyAuth();
    } else {
      console.error("Gpro: パスキー認証UIが未接続です");
    }
  }
});
