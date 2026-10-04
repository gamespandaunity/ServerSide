using UnityEngine;
using System.Collections;

public class CarGO {

	private long UnlockPrice;
	private long UpgradePrice;
	private int UpgradeLevel;
	private double Speed;
	private double Acceleration;
	private double Control;
	private double N2O;

	public long getUnlockPrice() {
		return UnlockPrice;
	}
	public void setUnlockPrice(long unlockPrice) {
		UnlockPrice = unlockPrice;
	}
	public long getUpgradePrice() {
		return UpgradePrice;
	}
	public void setUpgradePrice(long upgradePrice) {
		UpgradePrice = upgradePrice;
	}
	public int getUpgradeLevel() {
		return UpgradeLevel;
	}
	public void setUpgradeLevel(int upgradeLevel) {
		UpgradeLevel = upgradeLevel;
	}
	public double getSpeed() {
		return Speed;
	}
	public void setSpeed(double speed) {
		Speed = speed;
	}
	public double getAcceleration() {
		return Acceleration;
	}
	public void setAcceleration(double acceleration) {
		Acceleration = acceleration;
	}
	public double getControl() {
		return Control;
	}
	public void setControl(double control) {
		Control = control;
	}
	public double getN2O() {
		return N2O;
	}
	public void setN2O(double n2o) {
		N2O = n2o;
	}

}
