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
            Log.d("RECEIVER ", "Callback(): Pusty callback");
            Toast.makeText(context, "Callback(): Pusty callback" , Toast.LENGTH_LONG);

            return;
        }
        Callback cback = callback.get();
        if (cback == null) {

            Log.d("RECEIVER ", "Callback(): Pusty callback i ustawiony na null");
            Toast.makeText(context, "Callback(): Pusty callback i ustawiony na null" , Toast.LENGTH_LONG);

            callback = null;
            return;
        }
        if (DownloadManager.ACTION_DOWNLOAD_COMPLETE.equals(intent.getAction())) {

            Log.d("RECEIVER ", "Callback(): onReceive() - try");

            Toast.makeText(context, "Callback(): onReceive() - try" , Toast.LENGTH_LONG);

            try {
                Log.d("RECEIVER ", "Callback(): Download complete");

                Toast.makeText(context, "Callback(): Download complete" , Toast.LENGTH_LONG);

                cback.downloadCompleted();
            } catch (UnsatisfiedLinkError e) {
                // if app quits, callback can be not-null, but is invalid (C# side destroyed)
                // this is fine: we will pick the status next time app is launched
                // this is to due to race-condition: C# object destroyed, Java not yet
                Toast.makeText(context, "Exception " + e.getMessage(), Toast.LENGTH_LONG);
                Log.d("RECEIVER ", "Callback(): I/Unity - my log: Picking last download when restarted");
                callback = null;
            }
        }
    }
}
