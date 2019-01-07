package com.unity3d.backgrounddownload;

import java.lang.ref.WeakReference;
import android.app.DownloadManager;
import android.content.BroadcastReceiver;
import android.content.Context;
import android.content.Intent;
import android.util.Log;

public class CompletionReceiver extends BroadcastReceiver {
    public interface Callback {
        void downloadCompleted();
    }

    private static WeakReference<Callback> callback;

    public static void setCallback(Callback cback) {
        callback = new WeakReference(cback);
    }

    @Override
    public void onReceive(Context context, Intent intent) {
        if (callback == null)
            return;
        Callback cback = callback.get();
        if (cback == null) {
            callback = null;
            return;
        }
        if (DownloadManager.ACTION_DOWNLOAD_COMPLETE.equals(intent.getAction())) {
            try {
                cback.downloadCompleted();
            } catch (UnsatisfiedLinkError e) {
                // if app quits, callback can be not-null, but is invalid (C# side destroyed)
                // this is fine: we will pick the status next time app is launched
                // this is to due to race-condition: C# object destroyed, Java not yet
                Log.v("UPE_COMPLETION_RECEIVER", "Picking last download when restarted");
                callback = null;
            }
        }
    }
}
