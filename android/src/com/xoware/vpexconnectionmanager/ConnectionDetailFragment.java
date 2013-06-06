package com.xoware.vpexconnectionmanager;

import android.app.Activity;
import android.app.DialogFragment;
import android.app.Fragment;
import android.content.Context;
import android.os.Bundle;
import android.util.Log;
import android.view.KeyEvent;
import android.view.LayoutInflater;
import android.view.View;
import android.view.View.OnClickListener;
import android.view.ViewGroup;
import android.view.WindowManager;
import android.view.inputmethod.InputMethodManager;
import android.widget.Button;
import android.widget.EditText;
import android.widget.TextView;
import android.widget.TextView.OnEditorActionListener;

public class ConnectionDetailFragment extends Fragment implements OnClickListener, OnEditorActionListener {

	public static final String TAG = "VPExConnectionManager";
    public static final String ARG_ITEM_ID = "item_id";
    Button deleteButton, nfcButton;
    NFCCallback mCallback;
    String mItem;
    String mPassword;
    TextView cryptField;
    EditText passwordField;
    
	public interface NFCCallback {
		public void writeNFCTagForString(String s, String pw);
	}

    public ConnectionDetailFragment() {
    }

    @Override
    public void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        if (getArguments().containsKey(ARG_ITEM_ID)) {
            mItem = getArguments().getString(ARG_ITEM_ID);
        }
    }

    @Override
    public View onCreateView(LayoutInflater inflater, ViewGroup container,
            Bundle savedInstanceState) {
        View rootView = inflater.inflate(R.layout.fragment_connection_detail, container, false);
        if (mItem != null) {
            ((TextView) rootView.findViewById(R.id.connection_detail)).setText(mItem);
            passwordField = (EditText)rootView.findViewById(R.id.password_field);
            passwordField.setOnEditorActionListener(this);
            cryptField = (TextView)rootView.findViewById(R.id.crypt_field);
            deleteButton = (Button)rootView.findViewById(R.id.deleteButton);
            deleteButton.setOnClickListener(this);

            nfcButton = (Button)rootView.findViewById(R.id.nfcButton);
            nfcButton.setOnClickListener(this);
        }
        return rootView;
    }
    
    @Override
    public void onAttach(Activity activity) {

        super.onAttach(activity);

        try {
            mCallback = (NFCCallback) activity;
        }
        catch (ClassCastException e) {
            throw new ClassCastException(activity.toString() + " must implement " + NFCCallback.class.getName());
        }
    }
    
    public boolean onEditorAction(TextView v, int actionId, KeyEvent event)
    {
    	String password = passwordField.getText().toString();
    	SimpleCrypto crypto = new SimpleCrypto();
		String masterPw = IDManager.getInstance().id(MyApp.getContext());
        final InputMethodManager imm = (InputMethodManager) getActivity().getSystemService(Context.INPUT_METHOD_SERVICE);
        imm.hideSoftInputFromWindow(getView().getWindowToken(), 0);
        try {
			mPassword = crypto.encrypt(masterPw, password);
		} catch (Exception e) {
			// TODO Auto-generated catch block
			e.printStackTrace();
		}
        Log.d(TAG, "key = " + masterPw);
        Log.d(TAG, "crypted = " + mPassword);
    	cryptField.setText(mPassword);
    	return true;
    }
    
    public void onClick(View v) {
    	if (v == deleteButton)  {
    		DialogFragment newFragment = DeleteConfirmationDialogFragment.newInstance();
            Bundle arguments = new Bundle();
            arguments.putString(DeleteConfirmationDialogFragment.ARG_ITEM_ID, mItem);
            newFragment.setArguments(arguments);
            newFragment.show(getFragmentManager(), "dialog");
    	} else if (v == nfcButton)  {
    		Log.d(TAG, "nfc button clicked");
    		mCallback.writeNFCTagForString(mItem, mPassword);
    	}
    }
}
