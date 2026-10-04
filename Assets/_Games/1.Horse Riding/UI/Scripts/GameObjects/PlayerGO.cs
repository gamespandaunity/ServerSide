using UnityEngine;
using System.Collections;

public class PlayerGO {

	private int Rank;
	private long SilverCoins;
	private long GoldCoins;
	private ArrayList Cars;

	public int getRank() {
		return Rank;
	}
	public void setRank(int rank) {
		Rank = rank;
	}
	public long getSilverCoins() {
		return SilverCoins;
	}
	public void setSilverCoins(long silverCoins) {
		SilverCoins = silverCoins;
	}
	public long getGoldCoins() {
		return GoldCoins;
	}
	public void setGoldCoins(long goldCoins) {
		GoldCoins = goldCoins;
	}
	public ArrayList getCars() {
		return Cars;
	}
	public void setCars(ArrayList cars) {
		Cars = cars;
	}

}
