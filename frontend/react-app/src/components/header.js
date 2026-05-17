import Navbar from "./Navbar";
import SignInProfile from "./SignInProfile";
import "../styles/Header.css";

function Header() {
  return (
    <div className="header-container">
      <div></div>
      <SignInProfile />
    </div>
  );
}

export default Header;