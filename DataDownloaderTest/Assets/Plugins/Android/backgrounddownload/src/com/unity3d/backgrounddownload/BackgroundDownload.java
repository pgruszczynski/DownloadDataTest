package com.unity3d.backgrounddownload;

import android.app.Application;
import android.app.DownloadManager;
import android.app.Notification;
import android.app.NotificationChannel;
import android.app.NotificationManager;
import android.content.Context;
import android.database.Cursor;
import android.graphics.Bitmap;
import android.graphics.BitmapFactory;
import android.net.Uri;
import android.os.Build;
import android.os.SystemClock;
import android.support.v4.app.NotificationCompat;
import android.support.v4.app.NotificationManagerCompat;
import android.support.v4.media.session.MediaSessionCompat;

public class BackgroundDownload {
    public static BackgroundDownload create(String url, String destUri) {
        Uri uri = Uri.parse(url);
        Uri dest = Uri.parse(destUri);
        return new BackgroundDownload(uri, dest);
    }

    public static BackgroundDownload recreate(Context context, long id) {
        DownloadManager manager = (DownloadManager)context.getSystemService(Context.DOWNLOAD_SERVICE);
        DownloadManager.Query query = new DownloadManager.Query();
        query.setFilterById(id);
        Cursor cursor = manager.query(query);
        if (cursor.getCount() == 0)
            return null;
        cursor.moveToFirst();
        Uri url = Uri.parse(cursor.getString(cursor.getColumnIndex(DownloadManager.COLUMN_URI)));
        Uri dest = Uri.parse(cursor.getString(cursor.getColumnIndex(DownloadManager.COLUMN_LOCAL_URI)));
        cursor.close();
        return new BackgroundDownload(context, manager, id, url, dest);
    }

    private Uri downloadUri;
    private Uri destinationUri;
    private DownloadManager manager;
    private DownloadManager.Request request;
    private long id;
    private int totalFilesizeBytes;
    private boolean isFilesizeKnown;
    private String error;

    // notyfikacje
    private NotificationCompat.Builder notificationBuilder;
    private NotificationManager notificationManager;
    private NotificationChannel notificationChannel;
    // customowa download notyfikacja
    // ewentualnei dodać do res/values/strings.xml
    private CharSequence notificationName = "Questland notification";
    private final String DOWNLOAD_NOTIFICATION_CHANNEL_ID = "QUESTLAND_CHANNEL_ID";
    private final String DOWNLOAD_CHANNEL_DESCRIPTION = "Questland download description";
    private int notificationImportance = NotificationManager.IMPORTANCE_DEFAULT;
    private int notificationId = 9696;

    private Bitmap largeIcon;
    private android.support.v4.media.app.NotificationCompat.MediaStyle mediaStyle;
    private MediaSessionCompat mediaSessionCompat;

    private BackgroundDownload(Uri url, Uri dest) {
        downloadUri = url;
        destinationUri = dest;
        request = new DownloadManager.Request(url);
        request.setDestinationUri(dest);
        request.setNotificationVisibility(DownloadManager.Request.VISIBILITY_HIDDEN);

// domyslna download notyfikacja
//        request.setTitle("Questland")
//                .setDescription("Downloading additional files")
//                .setNotificationVisibility(DownloadManager.Request.VISIBILITY_VISIBLE_NOTIFY_COMPLETED);
    }

    private void createCustomNotificationLook(Context context){
        largeIcon = BitmapFactory.decodeResource(context.getResources(), R.drawable.icon_big);
        mediaStyle = new android.support.v4.media.app.NotificationCompat.MediaStyle();
        mediaSessionCompat = new MediaSessionCompat(context, "questland_media_session");
    }

    private void createCustomNotificationChannel(Context context){
        if(Build.VERSION.SDK_INT >= Build.VERSION_CODES.O){

            notificationChannel = new NotificationChannel(DOWNLOAD_NOTIFICATION_CHANNEL_ID, notificationName, notificationImportance);
            notificationChannel.setDescription(DOWNLOAD_CHANNEL_DESCRIPTION);

            notificationManager = context.getSystemService(NotificationManager.class);
            notificationManager.createNotificationChannel(notificationChannel);
        }
    }

    private BackgroundDownload(Context context, DownloadManager manager, long id, Uri url, Uri dest)
    {
        this.manager = manager;
        this.id = id;
        downloadUri = url;
        destinationUri = dest;

        //customowa notyfikacja po przywróceniu pobierania
//        createCustomNotificationChannel(context);
//        createCustomNotification(context, false);
    }

    private void createCustomNotification(Context context, boolean isNewDownload) {
        String notificationTitle = (isNewDownload) ? "Questland - download started " : "Questland - download restored";

        notificationBuilder = new NotificationCompat.Builder(context, DOWNLOAD_NOTIFICATION_CHANNEL_ID);
        notificationBuilder.setSmallIcon(R.drawable.icon)
                .setLargeIcon(largeIcon)
                //.setStyle(mediaStyle.setMediaSession(mediaSessionCompat.getSessionToken()))
                .setContentTitle(notificationTitle)
                .setPriority(NotificationCompat.PRIORITY_LOW)
                .setOnlyAlertOnce(true)
                .setOngoing(true);

        final NotificationManagerCompat notificationManagerCompat = NotificationManagerCompat.from(context);

        final int PROGRESS_MAX = 100;

        notificationBuilder.setProgress(PROGRESS_MAX, 0, false);
        notificationManagerCompat.notify(notificationId, notificationBuilder.build());

        new Thread(new Runnable() {
            @Override
            public void run() {

                while(getPercentageProgress() < PROGRESS_MAX){

                    int currentProgress = getPercentageProgress();

                    notificationBuilder.setProgress(PROGRESS_MAX, currentProgress, false)
                            .setContentText("Progress " + currentProgress + "%");

                    notificationManagerCompat.notify(notificationId, notificationBuilder.build());

                    try{
                        Thread.sleep(500);
                    }
                    catch(InterruptedException e){
                        e.printStackTrace();
                    }
                }

                notificationBuilder.setContentText("Questland  download completed.")
                        .setProgress(0,0,false)
                        .setOngoing(false);

                notificationManagerCompat.notify(notificationId, notificationBuilder.build());

            }
        }).start();
    }


    public void setAllowMetered(boolean allow) {
        request.setAllowedOverMetered(allow);
    }

    public void setAllowRoaming(boolean allow) {
        request.setAllowedOverRoaming(allow);
    }

    public long start(Context context) {
        manager = (DownloadManager)context.getSystemService(Context.DOWNLOAD_SERVICE);
        id = manager.enqueue(request);

        createCustomNotificationChannel(context);
        createCustomNotificationLook(context);
        createCustomNotification(context, true);

        return id;
    }

    public void addRequestHeader(String name, String value) {
        request.addRequestHeader(name, value);
    }

    public int checkFinished() {
        if (error != null)
            return -1;
        Uri uri = manager.getUriForDownloadedFile(id);
        if (uri != null)
            return 1;
        DownloadManager.Query query = new DownloadManager.Query();
        query.setFilterById(id);
        Cursor cursor = manager.query(query);
        if (cursor.getCount() == 0) {
            error = "Background download not found";
            return -1;
        }
        cursor.moveToFirst();
        int status = cursor.getInt(cursor.getColumnIndex(DownloadManager.COLUMN_STATUS));
        int reason = cursor.getInt(cursor.getColumnIndex(DownloadManager.COLUMN_REASON));
        cursor.close();
        switch (status)
        {
            case DownloadManager.STATUS_FAILED:
                error = reasonToError(reason);
                return -1;
            case DownloadManager.STATUS_SUCCESSFUL:
                return 1;
            default:
                return 0;
        }
    }

    public int getPercentageProgress(){
        return (int)(getProgress() * 100);
    }

    public float getProgress() {
        if (error != null)
            return 1.0f;
        Uri uri = manager.getUriForDownloadedFile(id);
        if (uri != null)
            return 1.0f;
        DownloadManager.Query query = new DownloadManager.Query();
        query.setFilterById(id);
        Cursor cursor = manager.query(query);
        if (cursor.getCount() == 0) {
            error = "Background download not found";
            return 1.0f;
        }
        cursor.moveToFirst();
        int downloaded = cursor.getInt(cursor.getColumnIndex(DownloadManager.COLUMN_BYTES_DOWNLOADED_SO_FAR));
        int total = cursor.getInt(cursor.getColumnIndex(DownloadManager.COLUMN_TOTAL_SIZE_BYTES));
        cursor.close();
        if (downloaded <= 0)
            return 0.0f;
        if (total <= 0)
            return -1.0f;
        float ret = downloaded / (float)total;
        if (ret < 1.0f)
            return ret;
        return 1.0f;
    }

    private void setTotalFilesizeBytes(int bytes){
        totalFilesizeBytes = bytes;
    }

    public int getTotalFilesizeBytes(){
        return 42;
    }

    public String getDownloadUrl() {
        return downloadUri.toString();
    }

    public String getDestinationUri() {
        return destinationUri.toString();
    }

    public String getError() {
        return error;
    }

    private static String reasonToError(int reason) {
        switch (reason) {
            case DownloadManager.ERROR_CANNOT_RESUME:
                return "Cannot resume";
            case DownloadManager.ERROR_DEVICE_NOT_FOUND:
                return "Device not found";
            case DownloadManager.ERROR_FILE_ALREADY_EXISTS:
                return "File already exists";
            case DownloadManager.ERROR_FILE_ERROR:
                return "File error";
            case DownloadManager.ERROR_HTTP_DATA_ERROR:
                return "HTTP data error";
            case DownloadManager.ERROR_INSUFFICIENT_SPACE:
                return "Insufficient space";
            case DownloadManager.ERROR_TOO_MANY_REDIRECTS:
                return "Too many redirects";
            case DownloadManager.ERROR_UNHANDLED_HTTP_CODE:
                return "Unhandled HTTP code";
            case DownloadManager.ERROR_UNKNOWN:
            default:
                return "Unkown error";
        }
    }

    public void remove() {
        if (checkFinished() == 0)
            error = "Aborted";
        manager.remove(id);
    }
}
