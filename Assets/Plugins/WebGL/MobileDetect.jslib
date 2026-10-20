
mergeInto(LibraryManager.library, {
    GproIsPhone: function () {
        return /iPhone|iPod|Android/i.test(
            navigator.userAgent
        ) ? 1 : 0;
    }
});
