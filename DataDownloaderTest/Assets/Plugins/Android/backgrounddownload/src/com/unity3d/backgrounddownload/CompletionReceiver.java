package com.unity3d.backgrounddownload;

import java.lang.ref.WeakReference;
import android.app.DownloadManager;
import android.content.BroadcastReceiver;
import android.content.Context;
import android.content.Intent;
import android.util.Log;
import android.widget.Toast;

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
        if (callback == null){

            Log.d("UNITY_QL_PLG_RECEIVER ", "Callback(): Pusty callback");
            return;
        }
        Callback cback = callback.get();
        if (cback == null) {

            Log.d("UNITY_QL_PLG_RECEIVER ", "Callback(): Pusty callback i ustawiony na null");
            callback = null;
            return;
        }
        if (DownloadManager.ACTION_DOWNLOAD_COMPLETE.equals(intent.getAction())) {

            Log.d("UNITY_QL_PLG_RECEIVER ", "Callback(): onReceive() - try");
            try {

                Log.d("UNITY_QL_PLG_RECEIVER ", "Callback(): Download complete");
                cback.downloadCompleted();
            } catch (UnsatisfiedLinkError e) {
                // if app quits, callback can be not-null, but is invalid (C# side destroyed)
                // this is fine: we will pick the status next time app is launched
                // this is to due to race-condition: C# object destroyed, Java not yet
                Toast.makeText(context, "Exception " + e.getMessage(), Toast.LENGTH_LONG);
                callback = null;
            }
        }
    }
}
