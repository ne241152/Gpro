mergeInto(LibraryManager.library, {
  GproShowPasskeyDoor: function () {
    if (typeof window.GproShowPasskeyDoor === "function") {
      window.GproShowPasskeyDoor();
    } else {
      console.error("Gpro: ドア用JavaScriptが未接続です");
    }
  },
  GproRequestPasskeyAuth: function () {
    if (typeof window.GproShowPasskeyAuth === "function") {
      window.GproShowPasskeyAuth();
    } else {
      console.error("Gpro: パスキー認証UIが未接続です");
    }
  }
});
